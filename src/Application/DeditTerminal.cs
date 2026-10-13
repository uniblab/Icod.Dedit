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
using Icod.Dedit.Presentation;
using Icod.Widgets;

internal sealed class DeditTerminal : IDeditTerminal {
	private readonly CursesSession session;
	private readonly WidgetHost host;
	private readonly WidgetTabStrip tabs = new( "workspace-tabs" );
	private readonly EditorShell? editor;
	private readonly List<CursesInputProtocolLease> leases = [];
	private string? activeKey;

	private DeditTerminal( CursesSession session, bool newDocument ) {
		this.session = session;
		this.host = new WidgetHost( session, WidgetTheme.Default );
		this.host.Attach( this.tabs );
		if ( newDocument ) this.editor = new EditorShell( this.host );
		else this.host.Focus( this.tabs );
		session.StandardScreen.WrapMode = CursesWrapMode.Clip;
	}

	internal static async ValueTask<IDeditTerminal> OpenAsync( bool newDocument ) {
		CursesSession session = await CursesSession.OpenAsync( new CursesSessionOptions { InputMode = CursesInputMode.Raw } );
		DeditTerminal? terminal = null;
		try {
			terminal = new DeditTerminal( session, newDocument );
			foreach ( CursesInputProtocolOptions options in new[] {
				new CursesInputProtocolOptions { MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents },
				new CursesInputProtocolOptions { BracketedPaste = true },
				new CursesInputProtocolOptions { KeyboardReportingMode = CursesKeyboardReportingMode.Disambiguated }
			} ) {
				var result = await session.AcquireInputProtocolsAsync( options );
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
		WidgetTab[] items = captions.Select( ( name, index ) => new WidgetTab( index.ToString( System.Globalization.CultureInfo.InvariantCulture ), name,
			IsModified: index == 0 && this.editor?.Document.IsModified == true, IsClosable: false ) ).ToArray();
		this.tabs.SetTabs( items, this.activeKey );
		this.tabs.Arrange( new CursesRectangle( Math.Min( 1, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 1 ) ), screen.Columns ) );
		this.editor?.Arrange();
		this.session.StandardScreen.Clear();
		this.Write( 0, "dedit  |  Ctrl+Q Exit" );
		if ( this.editor is null ) this.Write( 2, "Files are not loaded in this shell yet." );
		this.Write( screen.Rows - 1, this.editor is null ? "Use arrows on tabs to preview paths. Ctrl+Q exits." :
			"Untitled in memory; changes are lost on exit. Ctrl+Z/Y undo/redo. Ctrl+Q exits." );
		this.host.Render();
		await this.session.RefreshAsync( cancellationToken );
	}

	public void Process( WidgetInput input, TimeSpan elapsed ) {
		IReadOnlyList<WidgetAction> actions = this.editor is null ? this.host.Process( input, elapsed ).Actions : this.editor.Process( input, elapsed );
		foreach ( WidgetAction action in actions ) {
			if ( action is WidgetTabActivationRequestedAction selection ) this.activeKey = selection.TabKey;
		}
	}

	public void Resize() {
		this.session.SynchronizeDimensions(); this.editor?.Arrange(); this.session.Invalidate();
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
			try { this.editor?.Dispose(); }
			finally { await DisposeOwnedResourcesAsync( this.host, this.session ); }
		}
	}

	internal static async ValueTask DisposeOwnedResourcesAsync( IDisposable host, IAsyncDisposable session ) {
		try { host.Dispose(); }
		finally { await session.DisposeAsync(); }
	}
}
