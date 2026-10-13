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
	private readonly CursesSession session;
	private readonly WidgetHost host;
	private readonly WidgetMenuBar menu = DeditMenu.Create();
	private readonly WidgetTabStrip tabs = new( "workspace-tabs" );
	private readonly DeditClipboard clipboard;
	private readonly DocumentManager documents;
	private readonly List<string> openErrors = [];
	private readonly List<CursesInputProtocolLease> leases = [];
	private EditorShell? Editor => this.documents.Active?.Shell;

	private DeditTerminal( CursesSession session ) {
		this.session = session;
		this.host = new WidgetHost( session, WidgetTheme.Default );
		this.clipboard = new DeditClipboard( this.host );
		this.documents = new DocumentManager( this.host );
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
			DocumentFileService files = new( Environment.CurrentDirectory );
			string? firstKey = null;
			foreach ( string path in options.Paths ) {
				try {
					DocumentEntry opened = terminal.documents.Open( await files.OpenAsync( path, CancellationToken.None ) );
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
		DeditMenu.UpdateState( this.menu, this.Editor?.Editor );
		this.menu.Arrange( screen.Bounds );
		this.tabs.Arrange( new CursesRectangle( Math.Min( 1, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 1 ) ), screen.Columns ) );
		this.Editor?.Arrange();
		this.session.StandardScreen.Clear();
		this.Write( screen.Rows - 1, this.openErrors.Count > 0 ?
			$"Open error ({this.openErrors.Count}): {this.openErrors[ 0 ]}" :
			$"{DocumentManager.SanitizeDisplay( this.documents.Active?.Caption ?? "Untitled" )} in memory; changes are lost on exit. Ctrl+Z/Y undo/redo. Ctrl+Q exits." );
		this.host.Render();
		await this.session.RefreshAsync( cancellationToken );
	}

	public bool Process( WidgetInput input, TimeSpan elapsed ) {
		if ( IsQuitKey( input ) ) return RequestQuit( this.host, this.menu, this.documents, input, elapsed );
		if ( TryCycleFocus( this.host, input ) ) return false;
		IReadOnlyList<WidgetAction> actions = this.Editor is null ? this.host.Process( input, elapsed ).Actions : this.Editor.Process( input, elapsed );
		foreach ( WidgetAction action in actions ) {
			if ( action is WidgetTabActivationRequestedAction selection ) this.documents.Activate( selection.TabKey, focusEditor: false );
			else if ( action is WidgetClipboardRequestedAction clipboard ) this.clipboard.Complete( clipboard.Request );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.GoToFile } ) this.host.Focus( this.tabs );
			else if ( action is WidgetCheckedInvokedAction { CommandId: DeditMenu.Wrap } wrap && this.Editor is not null )
				DeditMenu.ApplyWrap( this.Editor.Editor, wrap.IsChecked );
			else if ( action is WidgetInvokedAction { CommandId: DeditMenu.About } )
				this.host.ShowModal( DeditMenu.CreateAboutDialog() );
			else if ( action is WidgetDialogCompletedAction completed && ConfirmedDiscard( completed ) ) return true;
		}
		return false;
	}

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
