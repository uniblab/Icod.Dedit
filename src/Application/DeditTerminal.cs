/*
	Icod.Dedit
	Adapts one DCurses session to the editor's shell loop.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Dedit.Application;

using Icod.DCurses;
using Icod.Dedit.CommandLine;
using Icod.Dedit.Documents;
using Icod.Dedit.Interaction;
using Icod.Dedit.Presentation;
using Icod.Widgets;

internal sealed class DeditTerminal : IDeditTerminal {
	private const string SaveAsDialogId = "dedit-save-as";
	private const string OverwriteDialogId = "dedit-confirm-overwrite";
	private readonly CursesSession session;
	private readonly WidgetHost host;
	private readonly WidgetMenuBar menu = DeditMenu.Create();
	private readonly WidgetTabStrip tabs = new( "workspace-tabs" );
	private readonly DeditClipboard clipboard;
	private readonly DocumentManager documents;
	private readonly DocumentFileService files;
	private readonly CloseCoordinator close;
	private readonly DocumentBatchCoordinator batch;
	private readonly ExternalChangeCoordinator external;
	private readonly DeditSearchCoordinator search;
	private readonly DeditGoToCoordinator goTo;
	private string? closeAfterSaveKey;
	private readonly LocalWidgetFileSystemProvider browserProvider = new();
	private readonly DeditSaveTargetProvider saveTargetProvider = new();
	private WidgetSaveTargetPicker? savePicker;
	private WidgetFilePicker? openPicker;
	private PendingOverwrite? pendingOverwrite;
	private DocumentEntry? saveEntry;
	private string? saveError;
	private readonly List<string> openErrors = [];
	private readonly List<CursesInputProtocolLease> leases = [];
	private EditorShell? Editor => this.documents.Active?.Shell;
	private sealed record PendingOverwrite( string DialogId, DocumentEntry Entry, ExistingSaveTarget Target,
		bool CloseAfterSave, bool BatchPicker );

	private DeditTerminal( CursesSession session ) {
		this.session = session;
		this.host = new WidgetHost( session, WidgetTheme.Default );
		this.clipboard = new DeditClipboard( this.host );
		this.documents = new DocumentManager( this.host );
		this.files = new DocumentFileService( Environment.CurrentDirectory );
		this.close = new CloseCoordinator( this.host, this.documents, this.files );
		this.batch = new DocumentBatchCoordinator( this.documents, this.files, this.close );
		this.external = new ExternalChangeCoordinator( this.host, this.documents, this.files );
		this.search = new DeditSearchCoordinator( this.host, this.documents );
		this.goTo = new DeditGoToCoordinator( this.host, this.documents );
		this.host.Attach( this.menu );
		this.host.Attach( this.tabs );
		session.StandardScreen.WrapMode = CursesWrapMode.Clip;
	}

	internal static async ValueTask<IDeditTerminal> OpenAsync( DeditOptions options ) {
		ArgumentNullException.ThrowIfNull( options );
		CursesSession session = await CursesSession.OpenAsync( new CursesSessionOptions { InputMode = CursesInputMode.Raw } );
		DeditTerminal? terminal = null;
		try {
			terminal = new DeditTerminal( session );
			string? firstKey = null;
			foreach ( string path in options.Paths ) {
				try {
					DocumentEntry opened = terminal.documents.Open( await terminal.files.OpenAsync( path, CancellationToken.None ) );
					firstKey ??= opened.Key;
				} catch ( Exception exception ) when ( exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException ) {
					terminal.openErrors.Add( DocumentManager.SanitizeDisplay( $"{path}: {exception.Message}" ) );
				}
			}
			if ( firstKey is null ) terminal.documents.OpenUntitled();
			else terminal.documents.Activate( firstKey );
			foreach ( CursesInputProtocolOptions protocolOptions in new[] {
				new CursesInputProtocolOptions { MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents },
				new CursesInputProtocolOptions { BracketedPaste = true },
				new CursesInputProtocolOptions { KeyboardReportingMode = CursesKeyboardReportingMode.Disambiguated }
			} ) {
				var result = await session.AcquireInputProtocolsAsync( protocolOptions );
				if ( result.IsAvailable ) terminal.leases.Add( result.GetRequiredValue() );
			}
			return terminal;
		} catch {
			if ( terminal is null ) await session.DisposeAsync();
			else await terminal.DisposeAsync();
			throw;
		}
	}

	public async ValueTask<DeditEvent> ReadAsync( CancellationToken cancellationToken ) {
		CursesEvent current = await this.session.ReadEventAsync( cancellationToken );
		if ( current.Kind == CursesEventKind.Input && current.Input is { } input ) return new DeditEvent( DeditEventKind.Input, WidgetInput.From( input ) );
		if ( current.Lifecycle is { } lifecycle ) return lifecycle.Kind switch {
			CursesLifecycleEventKind.Interrupt or CursesLifecycleEventKind.Termination => new DeditEvent( DeditEventKind.Stop ),
			CursesLifecycleEventKind.Resize or CursesLifecycleEventKind.Resumed => new DeditEvent( DeditEventKind.Resize ),
			_ => new DeditEvent( DeditEventKind.Timeout )
		};
		return new DeditEvent( DeditEventKind.Timeout );
	}

	public async ValueTask DrawAsync( IReadOnlyList<string> captions, TimeSpan elapsed, CancellationToken cancellationToken ) {
		CursesScreen screen = this.session.Screen;
		this.tabs.SetTabs( this.documents.Tabs, this.documents.Active?.Key );
		DeditMenu.UpdateState( this.menu, this.Editor?.Editor, this.documents.Active?.FullPath is not null, this.search.Options );
		this.menu.Arrange( screen.Bounds );
		this.tabs.Arrange( new CursesRectangle( Math.Min( 1, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 1 ) ), screen.Columns ) );
		this.Editor?.Arrange();
		this.goTo.Arrange();
		this.session.StandardScreen.Clear();
		this.Write( screen.Rows - 1, this.saveError is not null ? this.saveError : this.openErrors.Count > 0 ?
			$"Open error ({this.openErrors.Count}): {this.openErrors[ 0 ]}" :
			$"{DocumentManager.SanitizeDisplay( this.documents.Active?.Caption ?? "Untitled" )}; Ctrl+S saves. Ctrl+Q exits." );
		this.host.Render();
		await this.session.RefreshAsync( cancellationToken );
	}

	public async ValueTask<bool> ProcessAsync( WidgetInput input, TimeSpan elapsed ) {
		if ( IsQuitKey( input ) ) return CanQuitFromKey( this.host, this.menu, input, elapsed )
			&& await this.StartBatchAsync( BatchOperation.Exit );
		if ( TryCycleFocus( this.host, input ) ) return false;
		IReadOnlyList<WidgetAction> actions = this.Editor is null ? this.host.Process( input, elapsed ).Actions : this.Editor.Process( input, elapsed );
		Queue<WidgetAction> pending = new( actions );
		while ( pending.TryDequeue( out WidgetAction? action ) ) {
			if ( action is WidgetInvokedAction { CommandId: DeditMenu.New } ) this.documents.OpenUntitled();
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Open } ) await this.StartOpenAsync();
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Save } && this.documents.Active is { } saving ) {
				if ( saving.FullPath is null ) await this.StartSaveAsAsync( saving, closeAfterSave: false );
				else {
					try { await this.documents.SaveAsync( saving, this.files, CancellationToken.None ); this.saveError = null; }
					catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); await this.OfferExternalChangeAsync( saving ); }
				}
			}
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.SaveAll } ) {
				if ( await this.StartBatchAsync( BatchOperation.SaveAll ) ) return true;
			}
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.CloseAll } ) {
				if ( await this.StartBatchAsync( BatchOperation.CloseAll ) ) return true;
			}
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Exit } ) {
				if ( await this.StartBatchAsync( BatchOperation.Exit ) ) return true;
			}
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Find } && this.documents.Active is { } finding )
				this.search.Open( finding, allowReplace: false );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Replace } && this.documents.Active is { } replacing )
				this.search.Open( replacing, allowReplace: true );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.FindNext or DeditMenu.FindPrevious } findDirection &&
				this.documents.Active is { } navigating ) {
				if ( !this.search.HasQueryFor( navigating ) ) this.search.Open( navigating, allowReplace: false );
				else {
					try {
						SearchHit? hit = this.search.Repeat( navigating,
							findDirection.CommandId == DeditMenu.FindPrevious, CancellationToken.None );
						this.saveError = hit is null ? "Not found." : null;
					} catch ( Exception exception ) when ( IsSaveError( exception ) || exception is System.Text.RegularExpressions.RegexMatchTimeoutException
						or TimeoutException ) { this.SetSaveError( exception ); }
				}
			}
			else if ( action is WidgetSearchRequestedAction searchRequest && this.search.Matches( searchRequest ) ) {
				WidgetSearchResult result = this.search.CompleteRequest( searchRequest, elapsed, CancellationToken.None );
				this.saveError = result.Error is not null ? DocumentManager.SanitizeDisplay( result.Error ) :
					result.Found ? null : "Not found.";
			}
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.SaveAs } && this.documents.Active is { } savingAs )
				await this.StartSaveAsAsync( savingAs, closeAfterSave: false );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Close } && this.documents.Active is { } closing )
				this.close.Request( closing.Key );
			else if ( action is WidgetTabCloseRequestedAction tabClose )
				this.close.Request( tabClose.TabKey );
			else if ( action is WidgetBrowserLoadRequestedAction openLoad && this.openPicker is { } openBrowser && openLoad.WidgetId == openBrowser.Id )
				openBrowser.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( this.browserProvider, openLoad.Request, CancellationToken.None ) );
			else if ( action is WidgetPickerValidationRequestedAction validate && this.openPicker is { } validating && validate.WidgetId == validating.Id ) {
				validating.ApplyValidationResult( await WidgetPickerValidationLoader.ValidateAsync(
					this.browserProvider, validate.Request, CancellationToken.None ) );
				foreach ( WidgetAction emitted in this.host.DrainActions() ) pending.Enqueue( emitted );
			}
			else if ( action is WidgetDialogCompletedAction opened && this.openPicker is { } finishedOpen &&
				opened.WidgetId == finishedOpen.Id ) {
				this.openPicker = null;
				if ( opened.Result is { Outcome: WidgetDialogOutcome.Accepted, Value: IReadOnlyList<string> paths } ) {
					this.openErrors.Clear();
					foreach ( string path in paths ) {
						try { this.documents.Open( await this.files.OpenAsync( path, CancellationToken.None ) ); }
						catch ( Exception exception ) when ( IsSaveError( exception ) ) {
							this.openErrors.Add( DocumentManager.SanitizeDisplay( $"{path}: {exception.Message}" ) );
						}
					}
				}
			}
			else if ( action is WidgetBrowserLoadRequestedAction load && this.savePicker is { } browsing && load.WidgetId == browsing.Id )
				browsing.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( this.browserProvider, load.Request, CancellationToken.None ) );
			else if ( action is WidgetSaveTargetInspectionRequestedAction inspect && this.savePicker is { } inspecting && inspect.WidgetId == inspecting.Id ) {
				WidgetSaveTargetObservation observation = await this.saveTargetProvider.InspectTargetAsync(
					inspect.Request.Directory, inspect.Request.FileName, CancellationToken.None );
				inspecting.ApplyInspectionResult( new WidgetSaveTargetInspectionResult( inspect.Request, observation ) );
				foreach ( WidgetAction emitted in this.host.DrainActions() ) pending.Enqueue( emitted );
			}
			else if ( action is WidgetDialogCompletedAction completed && this.savePicker is { } finishedPicker && completed.WidgetId == finishedPicker.Id ) {
				DocumentEntry? entry = this.saveEntry;
				this.savePicker = null; this.saveEntry = null;
				bool closeAfterSave = this.closeAfterSaveKey == entry?.Key;
				this.closeAfterSaveKey = null;
				bool batchPicker = entry is not null && this.batch.SaveAsKey == entry.Key;
				if ( entry is null ) { if ( batchPicker ) this.batch.Abort(); continue; }
				WidgetSaveTargetSelection? selection = completed.Result is
					{ Outcome: WidgetDialogOutcome.Accepted, Value: WidgetSaveTargetSelection selected } ? selected : null;
				if ( selection?.ExistedAtInspection == true ) {
					try {
						ExistingSaveTarget target = await this.files.CaptureExistingAsync( selection.TargetPath, CancellationToken.None );
						string dialogId = OverwriteDialogId + "-" + Guid.NewGuid().ToString( "N" );
						this.host.ShowModal( WidgetMessageDialog.YesNo( dialogId, "Overwrite file?",
							$"Replace {DocumentManager.SanitizeDisplay( Path.GetFileName( target.FullPath ) )}?" ) );
						this.pendingOverwrite = new( dialogId, entry, target, closeAfterSave, batchPicker );
					}
					catch ( Exception exception ) when ( IsSaveError( exception ) ) {
						this.SetSaveError( exception );
						if ( await this.CompleteSaveAsSelectionAsync( entry, null, null, closeAfterSave, batchPicker ) ) return true;
					}
				} else if ( await this.CompleteSaveAsSelectionAsync( entry, selection?.TargetPath, null, closeAfterSave, batchPicker ) ) return true;
			}
			else if ( action is WidgetDialogCompletedAction overwrite && this.pendingOverwrite is { } pendingOverwrite &&
				overwrite.WidgetId == pendingOverwrite.DialogId ) {
				this.pendingOverwrite = null;
				bool confirmed = overwrite.Result.Outcome == WidgetDialogOutcome.Yes;
				if ( await this.CompleteSaveAsSelectionAsync( pendingOverwrite.Entry,
					confirmed ? pendingOverwrite.Target.FullPath : null, confirmed ? pendingOverwrite.Target : null,
					pendingOverwrite.CloseAfterSave, pendingOverwrite.BatchPicker ) ) return true;
			}
			else if ( action is WidgetDialogCompletedAction changed && this.external.Matches( changed ) ) {
				try {
					if ( await this.external.CompleteAsync( changed, CancellationToken.None ) == ExternalDecisionResult.Reloaded ) this.saveError = null;
				} catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); }
			}
			else if ( action is WidgetTabActivationRequestedAction tabActivation && this.documents.Activate( tabActivation.TabKey, focusEditor: false ) &&
				this.documents.Active is { } activated ) await this.OfferExternalChangeAsync( activated );
			else if ( action is WidgetClipboardRequestedAction clipboard ) this.clipboard.Complete( clipboard.Request );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.GoToLine } && this.documents.Active is { } goToEntry )
				this.goTo.Open( goToEntry );
			else if ( action is WidgetDialogValidationRequestedAction goToValidation && this.goTo.Validate( goToValidation ) ) {
				foreach ( WidgetAction emitted in this.host.DrainActions() ) pending.Enqueue( emitted );
			}
			else if ( action is WidgetDialogCompletedAction goToCompleted && this.goTo.Complete( goToCompleted ) ) { }
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.GoToFile } ) this.host.Focus( this.tabs );
			else if ( action is WidgetCheckedInvokedAction { CommandId: DeditMenu.SearchCase } matchCase )
				this.search.SetOptions( this.search.Options with { MatchCase = matchCase.IsChecked } );
			else if ( action is WidgetCheckedInvokedAction { CommandId: DeditMenu.SearchWholeWord } wholeWord )
				this.search.SetOptions( this.search.Options with { WholeWord = wholeWord.IsChecked } );
			else if ( action is WidgetCheckedInvokedAction { CommandId: DeditMenu.SearchRegex } regex )
				this.search.SetOptions( this.search.Options with { UseRegularExpression = regex.IsChecked } );
			else if ( action is WidgetCheckedInvokedAction { CommandId: DeditMenu.Wrap } wrap && this.Editor is not null )
				DeditMenu.ApplyWrap( this.Editor.Editor, wrap.IsChecked );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.About } )
				this.host.ShowModal( DeditMenu.CreateAboutDialog() );
			else if ( action is WidgetDialogCompletedAction searchClosed && this.search.Close( searchClosed ) ) { }
			else if ( action is WidgetDialogCompletedAction decision ) {
				if ( this.batch.IsActive ) {
					try {
						if ( await this.HandleBatchProgressAsync( await this.batch.CompleteDecisionAsync( decision, CancellationToken.None ) ) ) return true;
					}
					catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.batch.Abort(); this.SetSaveError( exception ); }
				} else {
					CloseResolution resolution;
					try { resolution = await this.close.CompleteAsync( decision, CancellationToken.None ); }
					catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); continue; }
					if ( resolution == CloseResolution.SaveAsRequired && this.close.SaveAsKey is { } key &&
						this.documents.Find( key ) is { } entry ) await this.StartSaveAsAsync( entry, closeAfterSave: true );
				}
			}
		}
		return false;
	}

	private async Task<bool> CompleteSaveAsSelectionAsync( DocumentEntry entry, string? path, ExistingSaveTarget? overwrite,
		bool closeAfterSave, bool batchPicker ) {
		bool saved = false;
		if ( path is not null ) {
			try {
				if ( overwrite is null ) await this.documents.SaveAsNewAsync( entry, this.files, path, CancellationToken.None );
				else await this.documents.SaveAsExistingAsync( entry, this.files, overwrite, CancellationToken.None );
				this.saveError = null;
				saved = true;
				if ( closeAfterSave && !entry.Shell.Document.IsModified ) this.documents.Close( entry.Key );
			} catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); }
		}
		if ( batchPicker ) {
			try { return await this.HandleBatchProgressAsync( await this.batch.CompleteSaveAsAsync( saved, CancellationToken.None ) ); }
			catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.batch.Abort(); this.SetSaveError( exception ); }
		}
		return false;
	}

	private async Task<bool> StartBatchAsync( BatchOperation operation ) {
		try { return await this.HandleBatchProgressAsync( await this.batch.StartAsync( operation, CancellationToken.None ) ); }
		catch ( Exception exception ) when ( IsSaveError( exception ) ) {
			this.batch.Abort(); this.SetSaveError( exception ); return false;
		}
	}

	private async Task<bool> HandleBatchProgressAsync( BatchProgress progress ) {
		if ( progress == BatchProgress.ExitReady ) return true;
		if ( progress == BatchProgress.SaveAsRequired && this.batch.SaveAsKey is { } key &&
			this.documents.Find( key ) is { } entry ) await this.StartSaveAsAsync( entry, closeAfterSave: false );
		return false;
	}

	private async Task StartOpenAsync() {
		string directory = this.documents.Active?.FullPath is { } path
			? Path.GetDirectoryName( path ) ?? Environment.CurrentDirectory : Environment.CurrentDirectory;
		WidgetFilePicker picker = new( "dedit-open-" + Guid.NewGuid().ToString( "N" ),
			this.browserProvider, directory, allowMultiple: true, [ WidgetFileFilter.AllFiles ] );
		this.openPicker = picker;
		this.host.ShowModal( picker );
		picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( this.browserProvider,
			picker.RequestDirectory( directory ), CancellationToken.None ) );
	}

	private async Task StartSaveAsAsync( DocumentEntry entry, bool closeAfterSave ) {
		string directory = entry.FullPath is null ? Environment.CurrentDirectory :
			Path.GetDirectoryName( entry.FullPath ) ?? Environment.CurrentDirectory;
		string name = entry.FullPath is null ? entry.Caption + ".txt" : Path.GetFileName( entry.FullPath );
		this.saveEntry = entry;
		this.closeAfterSaveKey = closeAfterSave ? entry.Key : null;
		WidgetSaveTargetPicker picker = new( SaveAsDialogId + "-" + Guid.NewGuid().ToString( "N" ), this.browserProvider, this.saveTargetProvider, directory, name );
		this.savePicker = picker;
		this.host.ShowModal( picker );
		picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( this.browserProvider,
			picker.RequestDirectory( directory ), CancellationToken.None ) );
	}

	private async Task OfferExternalChangeAsync( DocumentEntry entry ) {
		try {
			ExternalDecisionResult result = await this.external.CheckAsync( entry, CancellationToken.None );
			if ( result == ExternalDecisionResult.Missing )
				this.saveError = "File missing on disk; editor buffer kept. Use Save As.";
			else if ( result == ExternalDecisionResult.Retargeted )
				this.saveError = "File target changed on disk; editor buffer kept. Use Save As.";
		} catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); }
	}

	private static bool IsSaveError( Exception exception ) => exception is IOException or UnauthorizedAccessException
		or ArgumentException or NotSupportedException or InvalidOperationException;

	private void SetSaveError( Exception exception ) =>
		this.saveError = DocumentManager.SanitizeDisplay( $"Save error: {exception.Message}" );

	private static bool IsQuitKey( WidgetInput input ) => input.Kind == WidgetInputKind.Key
		&& input.KeyPhase is null or CursesKeyEventPhase.Press
		&& input.Modifiers.HasFlag( WidgetInputModifiers.Control )
		&& input.Character?.Value is 'q' or 'Q';

	internal static bool CanQuitFromKey( WidgetHost host, WidgetMenuBar menu, WidgetInput input, TimeSpan elapsed ) {
		ArgumentNullException.ThrowIfNull( host ); ArgumentNullException.ThrowIfNull( menu ); ArgumentNullException.ThrowIfNull( input );
		if ( !IsQuitKey( input ) ) return false;
		WidgetDispatchResult result = host.Process( input, elapsed );
		return !result.Handled && !menu.IsOpen;
	}

	internal static bool TryCycleFocus( WidgetHost host, WidgetInput input ) {
		ArgumentNullException.ThrowIfNull( host ); ArgumentNullException.ThrowIfNull( input );
		if ( input.Kind != WidgetInputKind.Key || input.KeyCode != WidgetKey.Function || input.FunctionKeyNumber != 6 ||
			input.Modifiers is not ( WidgetInputModifiers.None or WidgetInputModifiers.Shift ) ||
			input.KeyPhase is not ( null or CursesKeyEventPhase.Press ) ) return false;
		host.MoveFocus( input.Modifiers == WidgetInputModifiers.Shift ? CursesFocusDirection.Backward : CursesFocusDirection.Forward );
		return true;
	}

	public void Resize() {
		this.session.SynchronizeDimensions(); this.Editor?.Arrange(); this.session.Invalidate();
	}

	private void Write( int row, string text ) {
		CursesScreen screen = this.session.Screen;
		if ( row < 0 || row >= screen.Rows || screen.Columns == 0 ) return;
		screen.StandardWindow.Move( row, 0 );
		screen.StandardWindow.Write( CursesText.TruncateToColumns( text, screen.Columns, screen.TextWidthProvider ) );
	}

	public async ValueTask DisposeAsync() {
		try {
			try { this.session.StandardScreen.Clear(); await this.session.RefreshAsync(); }
			finally {
				for ( int index = this.leases.Count - 1; index >= 0; index-- ) await this.leases[ index ].DisposeAsync();
			}
		} finally {
			try { this.documents.Dispose(); }
			finally { await DisposeOwnedResourcesAsync( this.host, this.session ); }
		}
	}

	internal static async ValueTask DisposeOwnedResourcesAsync( IDisposable host, IAsyncDisposable session ) {
		try { host.Dispose(); }
		finally { await session.DisposeAsync(); }
	}
}
