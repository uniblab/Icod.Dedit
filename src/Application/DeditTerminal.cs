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
using Icod.Dedit.Presentation;
using Icod.Widgets;

internal sealed class DeditTerminal : IDeditTerminal {
	private const string QuitDialogId = "dedit-discard-and-quit";
	private const string SaveAsDialogId = "dedit-save-as";
	private readonly CursesSession session;
	private readonly WidgetHost host;
	private readonly WidgetMenuBar menu = DeditMenu.Create();
	private readonly WidgetTabStrip tabs = new( "workspace-tabs" );
	private readonly DeditClipboard clipboard;
	private readonly DocumentManager documents;
	private readonly DocumentFileService files;
	private readonly CloseCoordinator close;
	private string? closeAfterSaveKey;
	private readonly LocalWidgetFileSystemProvider browserProvider = new();
	private readonly DeditSaveTargetProvider saveTargetProvider = new();
	private WidgetSaveTargetPicker? savePicker;
	private DocumentEntry? saveEntry;
	private string? saveError;
	private readonly List<string> openErrors = [];
	private readonly List<CursesInputProtocolLease> leases = [];
	private EditorShell? Editor => this.documents.Active?.Shell;

	private DeditTerminal( CursesSession session ) {
		this.session = session;
		this.host = new WidgetHost( session, WidgetTheme.Default );
		this.clipboard = new DeditClipboard( this.host );
		this.documents = new DocumentManager( this.host );
		this.files = new DocumentFileService( Environment.CurrentDirectory );
		this.close = new CloseCoordinator( this.host, this.documents, this.files );
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
		DeditMenu.UpdateState( this.menu, this.Editor?.Editor, this.documents.Active?.FullPath is not null );
		this.menu.Arrange( screen.Bounds );
		this.tabs.Arrange( new CursesRectangle( Math.Min( 1, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 1 ) ), screen.Columns ) );
		this.Editor?.Arrange();
		this.session.StandardScreen.Clear();
		this.Write( screen.Rows - 1, this.saveError is not null ? this.saveError : this.openErrors.Count > 0 ?
			$"Open error ({this.openErrors.Count}): {this.openErrors[ 0 ]}" :
			$"{DocumentManager.SanitizeDisplay( this.documents.Active?.Caption ?? "Untitled" )}; Ctrl+S saves existing files. Ctrl+Q exits." );
		this.host.Render();
		await this.session.RefreshAsync( cancellationToken );
	}

	public async ValueTask<bool> ProcessAsync( WidgetInput input, TimeSpan elapsed ) {
		if ( IsQuitKey( input ) ) return RequestQuit( this.host, this.menu, this.documents, input, elapsed );
		if ( TryCycleFocus( this.host, input ) ) return false;
		IReadOnlyList<WidgetAction> actions = this.Editor is null ? this.host.Process( input, elapsed ).Actions : this.Editor.Process( input, elapsed );
		Queue<WidgetAction> pending = new( actions );
		while ( pending.TryDequeue( out WidgetAction? action ) ) {
			if ( action is WidgetInvokedAction { CommandId: DeditMenu.Save } && this.documents.Active is { } saving ) {
				try { await this.documents.SaveAsync( saving, this.files, CancellationToken.None ); this.saveError = null; }
				catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); }
			}
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.SaveAs } && this.documents.Active is { } savingAs )
				await this.StartSaveAsAsync( savingAs, closeAfterSave: false );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.Close } && this.documents.Active is { } closing )
				this.close.Request( closing.Key );
			else if ( action is WidgetTabCloseRequestedAction tabClose )
				this.close.Request( tabClose.TabKey );
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
				string? closeKey = this.closeAfterSaveKey;
				this.closeAfterSaveKey = null;
				if ( completed.Result is { Outcome: WidgetDialogOutcome.Accepted, Value: WidgetSaveTargetSelection selectedTarget } && entry is not null ) {
					try {
						await this.documents.SaveAsNewAsync( entry, this.files, selectedTarget.TargetPath, CancellationToken.None );
						this.saveError = null;
						if ( closeKey == entry.Key && !entry.Shell.Document.IsModified ) this.documents.Close( entry.Key );
					}
					catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); }
				}
			}
			else if ( action is WidgetTabActivationRequestedAction tabActivation ) this.documents.Activate( tabActivation.TabKey, focusEditor: false );
			else if ( action is WidgetClipboardRequestedAction clipboard ) this.clipboard.Complete( clipboard.Request );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.GoToFile } ) this.host.Focus( this.tabs );
			else if ( action is WidgetCheckedInvokedAction { CommandId: DeditMenu.Wrap } wrap && this.Editor is not null )
				DeditMenu.ApplyWrap( this.Editor.Editor, wrap.IsChecked );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.About } )
				this.host.ShowModal( DeditMenu.CreateAboutDialog() );
			else if ( action is WidgetDialogCompletedAction decision ) {
				CloseResolution resolution;
				try { resolution = await this.close.CompleteAsync( decision, CancellationToken.None ); }
				catch ( Exception exception ) when ( IsSaveError( exception ) ) { this.SetSaveError( exception ); continue; }
				if ( resolution == CloseResolution.SaveAsRequired && this.close.SaveAsKey is { } key &&
					this.documents.Find( key ) is { } entry ) await this.StartSaveAsAsync( entry, closeAfterSave: true );
				else if ( resolution == CloseResolution.Unrelated && ConfirmedDiscard( decision ) ) return true;
			}
		}
		return false;
	}


	private async Task StartSaveAsAsync( DocumentEntry entry, bool closeAfterSave ) {
		string directory = entry.FullPath is null ? Environment.CurrentDirectory :
			Path.GetDirectoryName( entry.FullPath ) ?? Environment.CurrentDirectory;
		string name = entry.FullPath is null ? entry.Caption + ".txt" : Path.GetFileName( entry.FullPath );
		this.saveEntry = entry;
		this.closeAfterSaveKey = closeAfterSave ? entry.Key : null;
		WidgetSaveTargetPicker picker = new( SaveAsDialogId, this.browserProvider, this.saveTargetProvider, directory, name );
		this.savePicker = picker;
		this.host.ShowModal( picker );
		picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( this.browserProvider,
			picker.RequestDirectory( directory ), CancellationToken.None ) );
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

	internal static bool RequestQuit( WidgetHost host, WidgetMenuBar menu, DocumentManager documents, WidgetInput input, TimeSpan elapsed ) {
		ArgumentNullException.ThrowIfNull( documents );
		if ( !CanQuitFromKey( host, menu, input, elapsed ) ) return false;
		int dirty = documents.Tabs.Count( tab => tab.IsModified );
		if ( dirty == 0 ) return true;
		host.ShowModal( WidgetMessageDialog.YesNo( QuitDialogId, "Unsaved changes",
			$"Discard changes in {dirty} modified document{( dirty == 1 ? "" : "s" )} and exit?" ) );
		return false;
	}

	internal static bool ConfirmedDiscard( WidgetDialogCompletedAction action ) =>
		action.WidgetId == QuitDialogId && action.Result.Outcome == WidgetDialogOutcome.Yes;

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
