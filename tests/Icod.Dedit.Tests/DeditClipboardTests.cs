/*
	Icod.Dedit.Tests
	Verifies application-owned clipboard routing for focused editor commands.
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

namespace Icod.Dedit.Tests;

using System.Text;
using Icod.DCurses;
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class DeditClipboardTests {
	[Fact]
	public void PointerCopyThenPasteOverSelectionCollapsesSelectionAndPastesAgain() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu );
		DeditMenuPresenter presenter = new( menu ); presenter.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host );
		shell.Process( WidgetInput.Paste( "copy" ), TimeSpan.Zero );
		shell.Process( Ctrl( 'a' ), TimeSpan.FromMilliseconds( 1 ) );
		presenter.Update( shell.Editor ); host.Render();

		Assert.Empty( Pointer( shell, CursesMouseAction.Press, 7, 0, 2 ) );
		Assert.Empty( Pointer( shell, CursesMouseAction.Release, 7, 0, 3 ) );
		host.Render(); Assert.True( menu.IsOpen );
		int column = menu.RenderBounds.Column + 2;
		int copyRow = menu.RenderBounds.Row + 5;
		Assert.Empty( Pointer( shell, CursesMouseAction.Press, column, copyRow, 4 ) );
		WidgetClipboardRequestedAction copy = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			Pointer( shell, CursesMouseAction.Release, column, copyRow, 5 ) ) );
		Assert.True( clipboard.Complete( copy.Request ) );

		presenter.Update( shell.Editor ); host.Render();
		Assert.Empty( Pointer( shell, CursesMouseAction.Press, 7, 0, 6 ) );
		Assert.Empty( Pointer( shell, CursesMouseAction.Release, 7, 0, 7 ) );
		host.Render(); Assert.True( menu.IsOpen );
		int pasteRow = menu.RenderBounds.Row + 6;
		Assert.Empty( Pointer( shell, CursesMouseAction.Press, column, pasteRow, 8 ) );
		WidgetClipboardRequestedAction pasteOverSelection = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			Pointer( shell, CursesMouseAction.Release, column, pasteRow, 9 ) ) );
		long revision = shell.Document.Revision;
		Assert.True( clipboard.Complete( pasteOverSelection.Request ) );
		Assert.Equal( "copy", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		Assert.Equal( revision, shell.Document.Revision ); Assert.False( shell.Editor.View.HasSelection ); Assert.Equal( 4, shell.Editor.View.Caret );

		presenter.Update( shell.Editor ); host.Render();
		Assert.Empty( Pointer( shell, CursesMouseAction.Press, 7, 0, 10 ) );
		Assert.Empty( Pointer( shell, CursesMouseAction.Release, 7, 0, 11 ) );
		host.Render(); Assert.True( menu.IsOpen );
		Assert.Empty( Pointer( shell, CursesMouseAction.Press, column, pasteRow, 12 ) );
		WidgetClipboardRequestedAction pasteAtCaret = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			Pointer( shell, CursesMouseAction.Release, column, pasteRow, 13 ) ) );
		Assert.True( clipboard.Complete( pasteAtCaret.Request ) );
		Assert.Equal( "copycopy", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public void MenuCopyAndDirectCutPasteShareClipboardAndUndoHistory() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host );
		shell.Process( WidgetInput.Paste( "first\nsecond" ), TimeSpan.FromMilliseconds( 1 ) );
		IReadOnlyList<WidgetAction> selection = shell.Process( Ctrl( 'a' ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Contains( selection, action => action is WidgetTextSelectionChangedAction );
		DeditMenu.UpdateState( menu, shell.Editor );
		Assert.Empty( shell.Process( Alt( 'e' ), TimeSpan.FromMilliseconds( 3 ) ) );
		WidgetClipboardRequestedAction copy = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'c' ) ), TimeSpan.FromMilliseconds( 4 ) ) ) );
		Assert.True( clipboard.Complete( copy.Request ) );
		Assert.Equal( "first\nsecond", clipboard.Text );
		WidgetClipboardRequestedAction cut = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( shell.Process( Ctrl( 'x' ), TimeSpan.FromMilliseconds( 5 ) ) ) );
		Assert.True( clipboard.Complete( cut.Request ) );
		Assert.Equal( 0, shell.Document.Length );
		WidgetClipboardRequestedAction paste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 6 ) ) ) );
		Assert.True( clipboard.Complete( paste.Request ) );
		Assert.Equal( "first\nsecond", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( Ctrl( 'z' ), TimeSpan.FromMilliseconds( 7 ) );
		Assert.Equal( 0, shell.Document.Length );
	}

	[Fact]
	public void EmptyClipboardAndChangedFocusCannotApplyPaste() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetTabStrip tabs = new( "tabs" ); host.Attach( tabs ); tabs.SetTabs( [ new WidgetTab( "0", "Untitled" ) ], "0" );
		tabs.Arrange( new CursesRectangle( 1, 0, 1, 50 ) );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host );
		WidgetClipboardRequestedAction empty = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Assert.False( clipboard.Complete( empty.Request ) );
		Assert.Equal( 0, shell.Document.Length );
		clipboard.SetText( "safe" );
		WidgetClipboardRequestedAction pending = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 2 ) ) ) );
		host.Focus( tabs );
		Assert.False( clipboard.Complete( pending.Request ) );
		Assert.Equal( 0, shell.Document.Length );
	}

	[Fact]
	public void ClipboardPasteKeepsTabsNewlinesAndControlTextAsOneEdit() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host );
		clipboard.SetText( "a\tb\n\u0011\u001a" );
		WidgetClipboardRequestedAction paste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Assert.True( clipboard.Complete( paste.Request ) );
		Assert.Equal( "a\tb\n\u0011\u001a", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		Assert.Empty( shell.Process( Ctrl( 'v' ) with { KeyPhase = CursesKeyEventPhase.Release }, TimeSpan.FromMilliseconds( 2 ) ) );
		Assert.Equal( "a\tb\n\u0011\u001a", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( Ctrl( 'z' ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( 0, shell.Document.Length );
	}

	[Fact]
	public void CopyPreservesControlCharactersThatRenderingSanitizes() {
		using WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		string exact = "a\u001b]52;c;payload\u0007\u009bb";
		shell.Process( WidgetInput.Paste( exact ), TimeSpan.Zero );
		shell.Process( Ctrl( 'a' ), TimeSpan.FromMilliseconds( 1 ) );

		WidgetClipboardRequestedAction copy = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'c' ), TimeSpan.FromMilliseconds( 2 ) ) ) );

		Assert.Equal( WidgetClipboardOperation.Copy, copy.Request.Operation );
		Assert.Equal( exact, copy.Request.Text );
	}

	[Fact]
	public void ModalBlocksClipboardShortcutAndPreservesClipboard() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host ); clipboard.SetText( "kept" );
		host.ShowModal( DeditMenu.CreateAboutDialog() );
		Assert.Empty( shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 1 ) ) );
		Assert.Equal( 0, shell.Document.Length );
		Assert.Equal( "kept", clipboard.Text );
	}

	[Fact]
	public async Task TerminalClipboardReadOverridesFallbackAndCopyWritesBestEffort() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		string? written = null;
		DeditClipboard clipboard = new( host,
			write: ( text, _ ) => { written = text; return ValueTask.CompletedTask; },
			read: ( _, _ ) => ValueTask.FromResult( Encoding.UTF8.GetBytes( "terminal" ) ) );
		shell.Process( WidgetInput.Paste( "local" ), TimeSpan.Zero );
		shell.Editor.View.SetSelection( 0, 5 );
		WidgetClipboardRequestedAction copy = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'c' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Assert.True( await clipboard.CompleteAsync( copy.Request ) );
		Assert.Equal( "local", written );
		shell.Editor.View.SetSelection( 5, 5 );
		WidgetClipboardRequestedAction paste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 2 ) ) ) );
		Assert.True( await clipboard.CompleteAsync( paste.Request ) );
		Assert.Equal( "localterminal", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public async Task TerminalTimeoutUsesLocalClipboardAndStaleReadCannotPaste() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetTabStrip tabs = new( "tabs" ); host.Attach( tabs ); tabs.SetTabs( [ new WidgetTab( "0", "Untitled" ) ], "0" );
		tabs.Arrange( new CursesRectangle( 1, 0, 1, 50 ) );
		using EditorShell shell = new( host );
		DeditClipboard fallback = new( host, read: ( _, _ ) => throw new TimeoutException() );
		fallback.SetText( "kept" );
		WidgetClipboardRequestedAction first = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.Zero ) ) );
		Assert.True( await fallback.CompleteAsync( first.Request ) );
		Assert.Equal( "kept", shell.Document.Read( 0, 4 ) );
		TaskCompletionSource<byte[]> pending = new( TaskCreationOptions.RunContinuationsAsynchronously );
		DeditClipboard delayed = new( host, read: ( _, _ ) => new ValueTask<byte[]>( pending.Task ) );
		WidgetClipboardRequestedAction second = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Task<bool> completion = delayed.CompleteAsync( second.Request ).AsTask();
		host.Focus( tabs );
		pending.SetResult( Encoding.UTF8.GetBytes( "late" ) );
		Assert.False( await completion );
		Assert.Equal( "kept", shell.Document.Read( 0, 4 ) );
	}

	[Fact]
	public void NoSelectionCopyAndLocalPasteRetainExactLineEndingAndUndo() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host );
		shell.Process( WidgetInput.Paste( "one\r\ntwo" ), TimeSpan.Zero );
		shell.Editor.View.SetSelection( 1, 1 );
		WidgetClipboardRequestedAction copy = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'c' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Assert.True( copy.Request.IsLinewise );
		Assert.Equal( "one\r\n", copy.Request.Text );
		Assert.True( clipboard.Complete( copy.Request ) );
		shell.Editor.View.SetSelection( 6, 6 );
		WidgetClipboardRequestedAction paste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 2 ) ) ) );
		Assert.True( clipboard.Complete( paste.Request ) );
		Assert.Equal( "one\r\none\r\ntwo", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( Ctrl( 'z' ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( "one\r\ntwo", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public async Task MatchingTerminalReadRetainsLinewiseButDifferentTextIsPlain() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		string terminalText = "one\n";
		DeditClipboard clipboard = new( host, read: ( _, _ ) => ValueTask.FromResult( Encoding.UTF8.GetBytes( terminalText ) ) );
		shell.Process( WidgetInput.Paste( "one\ntwo" ), TimeSpan.Zero );
		shell.Editor.View.SetSelection( 0, 0 );
		WidgetClipboardRequestedAction copy = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'c' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Assert.True( await clipboard.CompleteAsync( copy.Request ) );
		shell.Editor.View.SetSelection( 5, 5 );
		WidgetClipboardRequestedAction linewisePaste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 2 ) ) ) );
		Assert.True( await clipboard.CompleteAsync( linewisePaste.Request ) );
		Assert.Equal( "one\none\ntwo", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( Ctrl( 'z' ), TimeSpan.FromMilliseconds( 3 ) );
		terminalText = "external";
		shell.Editor.View.SetSelection( 5, 5 );
		WidgetClipboardRequestedAction plainPaste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 4 ) ) ) );
		Assert.True( await clipboard.CompleteAsync( plainPaste.Request ) );
		Assert.Equal( "one\ntexternalwo", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public void NoSelectionCutIsUndoableAndProgrammaticClipboardTextIsPlain() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); DeditClipboard clipboard = new( host );
		shell.Process( WidgetInput.Paste( "alpha\nbeta" ), TimeSpan.Zero );
		shell.Editor.View.SetSelection( 2, 2 );
		WidgetClipboardRequestedAction cut = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'x' ), TimeSpan.FromMilliseconds( 1 ) ) ) );
		Assert.True( cut.Request.IsLinewise );
		Assert.True( clipboard.Complete( cut.Request ) );
		Assert.Equal( "beta", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( Ctrl( 'z' ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "alpha\nbeta", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		clipboard.SetText( "X" );
		shell.Editor.View.SetSelection( 2, 2 );
		WidgetClipboardRequestedAction paste = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single(
			shell.Process( Ctrl( 'v' ), TimeSpan.FromMilliseconds( 3 ) ) ) );
		Assert.True( clipboard.Complete( paste.Request ) );
		Assert.Equal( "alXpha\nbeta", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	private static WidgetInput Ctrl( char key ) => WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( key ) );
	private static WidgetInput Alt( char key ) => WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( key ) );
	private static IReadOnlyList<WidgetAction> Pointer( EditorShell shell, CursesMouseAction action, int column, int row, int milliseconds ) =>
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( action, CursesMouseButton.Primary, column, row ) ), TimeSpan.FromMilliseconds( milliseconds ) );
}
