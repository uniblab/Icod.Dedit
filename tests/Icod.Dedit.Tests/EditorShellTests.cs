/*
	Icod.Dedit.Tests
	Integration tests for the in-memory editor shell.
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
using Icod.Dedit.Configuration;
using Icod.Dedit.Documents;
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class EditorShellTests {
	[Fact]
	public void UntitledEditorEditsAndUndoTracksSavedCheckpoint() {
		using WidgetHost host = new( new CursesScreen( 32, 8 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); shell.Arrange();
		Assert.Same( shell.Editor, host.FocusedWidget ); Assert.Equal( "", shell.Document.Read( 0, 0 ) );
		shell.Process( WidgetInput.TextInput( new Rune( 'a' ) ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Process( WidgetInput.TextInput( new Rune( '\u0301' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "a\u0301", shell.Document.Read( 0, 2 ) ); Assert.True( shell.Document.IsModified );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ), TimeSpan.FromSeconds( 1 ) );
		Assert.Equal( "", shell.Document.Read( 0, 0 ) ); Assert.False( shell.Document.IsModified );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'y' ) ), TimeSpan.FromSeconds( 2 ) );
		Assert.Equal( "a\u0301", shell.Document.Read( 0, 2 ) ); Assert.True( shell.Document.IsModified );
	}

	[Fact]
	public void TinyViewportKeepsDocumentWithoutAllocatingScreenForText() {
		CursesScreen screen = new( 7, 3 ); using WidgetHost host = new( screen, WidgetTheme.Default );
		using EditorShell shell = new( host, "長い行\nsecond" ); shell.Arrange();
		Assert.Equal( "長い行\nsecond", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Editor.View.SetSelection( 0, 0 ); screen.Resize( 2, 3 ); shell.Arrange(); host.Render();
		Assert.True( shell.Editor.Bounds.Rows <= host.Screen.Rows );
		Assert.Equal( "長", screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.True( screen.StandardWindow.GetCell( 2, 1 ).IsContinuation );
	}

	[Fact]
	public void EditorShowsLineNumbersAndTracksCurrentLogicalLine() {
		WidgetTheme theme = PresentationTheme(); using WidgetHost host = new( new CursesScreen( 20, 6 ), theme );
		using EditorShell shell = new( host, "first\nsecond" ); host.Render();

		Assert.Equal( "1", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutterCurrentLine ), host.Screen.StandardWindow.GetCell( 2, 0 ).Style );
		Assert.Equal( "2", host.Screen.StandardWindow.GetCell( 3, 0 ).Content );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutter ), host.Screen.StandardWindow.GetCell( 3, 0 ).Style );
		Assert.Equal( "f", host.Screen.StandardWindow.GetCell( 2, 2 ).Content );

		shell.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); host.Render();

		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutter ), host.Screen.StandardWindow.GetCell( 2, 0 ).Style );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutterCurrentLine ), host.Screen.StandardWindow.GetCell( 3, 0 ).Style );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorCurrentLine ), host.Screen.StandardWindow.GetCell( 3, 2 ).Style );
	}

	[Fact]
	public void WrappedEditorShowsOneNumberAndCurrentStyleAcrossContinuations() {
		WidgetTheme theme = PresentationTheme(); using WidgetHost host = new( new CursesScreen( 6, 6 ), theme );
		DeditPreferences preferences = new( 4, 4, true, TextFileEncoding.Utf8 );
		using EditorShell shell = new( host, "abcdef\nz", preferences: preferences ); host.Render();

		Assert.Equal( "1", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.True( host.Screen.StandardWindow.GetCell( 3, 0 ).IsBlank );
		Assert.Equal( "2", host.Screen.StandardWindow.GetCell( 4, 0 ).Content );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutterCurrentLine ), host.Screen.StandardWindow.GetCell( 2, 0 ).Style );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutterCurrentLine ), host.Screen.StandardWindow.GetCell( 3, 0 ).Style );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorGutter ), host.Screen.StandardWindow.GetCell( 4, 0 ).Style );
	}

	[Fact]
	public void EditorShowsAutoHiddenScrollbarOnlyWhenDocumentOverflows() {
		using WidgetHost host = new( new CursesScreen( 12, 6 ), PresentationTheme() );
		using EditorShell shell = new( host, "one\ntwo" ); host.Render();

		Assert.NotEqual( "▲", host.Screen.StandardWindow.GetCell( 2, 11 ).Content );
		shell.Editor.View.SetSelection( shell.Document.Length, shell.Document.Length );
		shell.Process( WidgetInput.Paste( "\nthree\nfour" ), TimeSpan.Zero ); host.Render();

		Assert.Equal( "▲", host.Screen.StandardWindow.GetCell( 2, 11 ).Content );
		Assert.Equal( "▼", host.Screen.StandardWindow.GetCell( 4, 11 ).Content );
	}

	[Fact]
	public void NoWrapOverflowComposesBothScrollbarsUntilWrappingIsEnabled() {
		using WidgetHost host = new( new CursesScreen( 12, 6 ), PresentationTheme() );
		using EditorShell shell = new( host, "abcdefghijklmnop\nsecond\nthird" ); host.Render();

		Assert.Equal( "▲", host.Screen.StandardWindow.GetCell( 2, 11 ).Content );
		Assert.Equal( "▼", host.Screen.StandardWindow.GetCell( 3, 11 ).Content );
		Assert.Equal( "◄", host.Screen.StandardWindow.GetCell( 4, 2 ).Content );
		Assert.Equal( "►", host.Screen.StandardWindow.GetCell( 4, 10 ).Content );
		Assert.True( host.Screen.StandardWindow.GetCell( 4, 11 ).IsBlank );
		( long anchor, long caret, long horizontal, long vertical ) before = ( shell.Editor.View.Anchor,
			shell.Editor.View.Caret, shell.Editor.HorizontalScrollRange.Offset, shell.Editor.VerticalScrollRange.Offset );

		IReadOnlyList<WidgetAction> press = shell.Process( WidgetInput.PointerInput( new CursesMouseEvent(
			CursesMouseAction.Press, CursesMouseButton.Primary, 11, 4 ) ), TimeSpan.Zero );
		IReadOnlyList<WidgetAction> release = shell.Process( WidgetInput.PointerInput( new CursesMouseEvent(
			CursesMouseAction.Release, CursesMouseButton.Primary, 11, 4 ) ), TimeSpan.FromMilliseconds( 1 ) );

		Assert.Empty( press ); Assert.Empty( release );
		Assert.Equal( before, ( shell.Editor.View.Anchor, shell.Editor.View.Caret,
			shell.Editor.HorizontalScrollRange.Offset, shell.Editor.VerticalScrollRange.Offset ) );

		shell.Editor.View.WrapMode = CursesTextWrapMode.Word; host.Render();

		Assert.NotEqual( "◄", host.Screen.StandardWindow.GetCell( 4, 2 ).Content );
		Assert.NotEqual( "►", host.Screen.StandardWindow.GetCell( 4, 10 ).Content );
	}

	[Fact]
	public void ControlDownScrollsDeditEditorByOneWrappedDisplayedRow() {
		using WidgetHost host = new( new CursesScreen( 8, 6 ), PresentationTheme() );
		DeditPreferences preferences = new( 4, 4, true, TextFileEncoding.Utf8 );
		using EditorShell shell = new( host, "abcdefghij\nsecond\nthird", preferences: preferences ); host.Render();

		shell.Process( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Control ), TimeSpan.Zero ); host.Render();

		Assert.Equal( 1, shell.Editor.VerticalScrollRange.Offset );
		Assert.Equal( 0, shell.Editor.View.VerticalOffset );
	}

	[Fact]
	public void DecorativeGutterClickLeavesSelectionUnchanged() {
		using WidgetHost host = new( new CursesScreen( 20, 6 ), PresentationTheme() );
		using EditorShell shell = new( host, "first\nsecond" ); shell.Editor.View.SetSelection( 1, 4 ); host.Render();

		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press,
			CursesMouseButton.Primary, 0, 2 ) ), TimeSpan.Zero );
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Move,
			CursesMouseButton.Primary, 8, 3 ) ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release,
			CursesMouseButton.Primary, 8, 3 ) ), TimeSpan.FromMilliseconds( 2 ) );

		Assert.Equal( ( 1L, 4L ), ( shell.Editor.View.Anchor, shell.Editor.View.Caret ) );
	}

	[Fact]
	public void IndentationDefaultsToFourSpacesAndOneUndoGroup() {
		using WidgetHost host = new( new CursesScreen( 32, 8 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "one\ntwo" ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Editor.View.SetSelection( 0, shell.Document.Length );
		shell.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "    one\n    two", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( "one\ntwo", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public void IndentationCanChangeForOneEditorWithoutEditingExistingText() {
		using WidgetHost host = new( new CursesScreen( 32, 8 ), WidgetTheme.Default );
		using EditorShell first = new( host );
		first.Process( WidgetInput.Paste( "one\ntwo" ), TimeSpan.Zero );
		using ( WidgetTextSnapshot snapshot = first.Document.CreateSnapshot() ) first.Document.MarkSaved( snapshot );
		first.SetIndentationWidth( 2 );
		Assert.Equal( 2, first.IndentationWidth );
		Assert.False( first.Document.IsModified );
		Assert.Equal( "one\ntwo", first.Document.Read( 0, checked( (int)first.Document.Length ) ) );
		first.Editor.View.SetSelection( 0, first.Document.Length );
		first.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.FromSeconds( 1 ) );
		Assert.Equal( "  one\n  two", first.Document.Read( 0, checked( (int)first.Document.Length ) ) );
		first.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ), TimeSpan.FromSeconds( 2 ) );
		Assert.False( first.Document.IsModified );
		Assert.Equal( "one\ntwo", first.Document.Read( 0, checked( (int)first.Document.Length ) ) );
	}

	[Theory]
	[InlineData( "first\r\nsecond", "first\r\nsecond\r\n" )]
	[InlineData( "first\rsecond", "first\rsecond\r" )]
	public void InitialTextUsesItsFirstTerminatorForFutureEnter( string initial, string expected ) {
		using WidgetHost host = new( new CursesScreen( 32, 8 ), WidgetTheme.Default );
		using EditorShell shell = new( host, initial );
		shell.Editor.View.SetSelection( shell.Document.Length, shell.Document.Length );
		shell.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		Assert.Equal( expected, shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public void UnicodeMovementAndMixedNewlinesPreserveStoredText() {
		using WidgetHost host = new( new CursesScreen( 18, 7 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		string exact = "a\u0301界👩‍💻\r\n\tshort\nlast";
		shell.Process( WidgetInput.Paste( exact ), TimeSpan.FromMilliseconds( 1 ) );
		long beforeCrLf = exact.IndexOf( "\r\n", StringComparison.Ordinal );
		shell.Editor.View.SetSelection( beforeCrLf, beforeCrLf );
		shell.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( exact.IndexOf( "👩", StringComparison.Ordinal ), shell.Editor.View.Caret );
		shell.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( beforeCrLf, shell.Editor.View.Caret );
		Assert.Equal( exact, shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		host.Render();
	}

	[Fact]
	public void PreferredVisualColumnSurvivesShortIntermediateLine() {
		using WidgetHost host = new( new CursesScreen( 24, 7 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "12345\nx\n12345" ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Editor.View.SetSelection( 4, 4 );
		shell.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( 7, shell.Editor.View.Caret );
		shell.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( 12, shell.Editor.View.Caret );
	}

	[Fact]
	public void LongLineRemainsExactAcrossWrapAndTinyResize() {
		CursesScreen screen = new( 40, 8 ); using WidgetHost host = new( screen, WidgetTheme.Default );
		using EditorShell shell = new( host );
		string exact = new string( '界', 4_000 ) + "\r\nfinal";
		shell.Process( WidgetInput.Paste( exact ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Editor.View.WrapMode = CursesTextWrapMode.NoWrap; host.Render();
		screen.Resize( 9, 4 ); shell.Arrange(); host.Render();
		shell.Editor.View.WrapMode = CursesTextWrapMode.TextElement; host.Render();
		screen.Resize( 4, 3 ); shell.Arrange(); host.Render();
		Assert.Equal( exact, shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		Assert.Equal( 1, shell.Editor.Bounds.Rows );
	}

	[Fact]
	public void ColdViewportProjectionHasBoundedAllocationForMillionUnitLine() {
		using WidgetHost host = new( new CursesScreen( 80, 24 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); host.Render();
		string text = new( 'x', 1_000_000 );
		WidgetTextEditResult edit = shell.Document.Apply( [ new WidgetTextEdit( 0, 0, text ) ], shell.Document.Revision,
			new WidgetTextEditContext( Guid.Empty, 0, 0, 1_000_000, 1_000_000, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );
		Assert.Equal( WidgetTextEditStatus.Applied, edit.Status );
		long before = GC.GetAllocatedBytesForCurrentThread(); host.Render();
		Assert.Equal( "1", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( "x", host.Screen.StandardWindow.GetCell( 2, 2 ).Content );
		Assert.InRange( GC.GetAllocatedBytesForCurrentThread() - before, 0, 5_000_000 );
	}

	[Fact]
	public void FirstTypedCharacterAppearsAfterInitialRender() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); host.Render();
		shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromMilliseconds( 1 ) );
		host.Render();
		Assert.Equal( "1", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( "x", host.Screen.StandardWindow.GetCell( 2, 2 ).Content );
	}

	[Fact]
	public void SelectionReplacementOfCombiningClusterIsOneUndoableEdit() {
		using WidgetHost host = new( new CursesScreen( 24, 6 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "one e\u0301 two" ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Editor.View.SetSelection( 4, 6 );
		shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "one x two", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( "one e\u0301 two", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'y' ) ), TimeSpan.FromMilliseconds( 4 ) );
		Assert.Equal( "one x two", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
	}

	[Fact]
	public void SavedSnapshotIdentityReturnsAfterUndoAndRedirtiesAfterRedo() {
		using WidgetHost host = new( new CursesScreen( 24, 6 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.FromMilliseconds( 1 ) );
		using ( WidgetTextSnapshot snapshot = shell.Document.CreateSnapshot() ) shell.Document.MarkSaved( snapshot );
		Assert.False( shell.Document.IsModified );
		shell.Process( WidgetInput.TextInput( new Rune( '!' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.True( shell.Document.IsModified );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( "draft", shell.Document.Read( 0, 5 ) ); Assert.False( shell.Document.IsModified );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'y' ) ), TimeSpan.FromMilliseconds( 4 ) );
		Assert.Equal( "draft!", shell.Document.Read( 0, 6 ) ); Assert.True( shell.Document.IsModified );
	}

	[Fact]
	public void PointerHitTestAfterResizeUsesUnicodeCellColumns() {
		CursesScreen screen = new( 20, 5 ); using WidgetHost host = new( screen, WidgetTheme.Default );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "界z" ), TimeSpan.FromMilliseconds( 1 ) ); host.Render();
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press,
			CursesMouseButton.Primary, 4, 2 ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( 1, shell.Editor.View.Caret );
		screen.Resize( 6, 4 ); shell.Arrange(); host.Render();
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press,
			CursesMouseButton.Primary, 2, 2 ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( 0, shell.Editor.View.Caret );
		Assert.Equal( "界z", shell.Document.Read( 0, 2 ) );
	}

	[Fact]
	public void PointerDragSurvivesUnchangedShellArrangeBetweenEvents() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default );
		using EditorShell shell = new( host, "abcdef" ); host.Render();

		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press,
			CursesMouseButton.Primary, 3, 2 ) ), TimeSpan.Zero );
		Assert.Equal( ( 1L, 1L ), ( shell.Editor.View.Anchor, shell.Editor.View.Caret ) );
		shell.Arrange(); host.Render();
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Move,
			CursesMouseButton.Primary, 6, 2 ) ), TimeSpan.FromMilliseconds( 1 ) );
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release,
			CursesMouseButton.Primary, 6, 2 ) ), TimeSpan.FromMilliseconds( 2 ) );

		Assert.Equal( ( 1L, 4L ), ( shell.Editor.View.Anchor, shell.Editor.View.Caret ) );
	}

	[Fact]
	public void TerminalControlCharactersStayExactInTheDocumentButNeverReachRenderedCells() {
		using WidgetHost host = new( new CursesScreen( 36, 7 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		string exact = "left\u001b]52;c;payload\u0007\u009bright\tend";
		shell.Process( WidgetInput.Paste( exact ), TimeSpan.Zero );
		host.Render();

		Assert.Equal( exact, shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		for ( int row = 0; row < host.Screen.Rows; row++ )
			for ( int column = 0; column < host.Screen.Columns; column++ )
					Assert.DoesNotContain( host.Screen.StandardWindow.GetCell( row, column ).Content, char.IsControl );
	}

	[Fact]
	public void OpeningAModalDuringPointerSelectionPreventsFurtherEditorSelection() {
		using WidgetHost host = new( new CursesScreen( 36, 8 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "first line\nsecond line" ), TimeSpan.Zero );
		host.Render();
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press,
			CursesMouseButton.Primary, 1, 2 ) ), TimeSpan.FromMilliseconds( 1 ) );
		long before = shell.Editor.View.Caret;
		host.ShowModal( WidgetMessageDialog.Ok( "pointer-modal", "Modal", "Selection is suspended." ) );

		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Move,
			CursesMouseButton.Primary, 30, 7 ) ), TimeSpan.FromMilliseconds( 2 ) );
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release,
			CursesMouseButton.Primary, 30, 7 ) ), TimeSpan.FromMilliseconds( 3 ) );

		Assert.Equal( before, shell.Editor.View.Caret );
	}

	[Fact]
	public void MillionUnitPasteSurvivesTinyResizeAndUndoesAsOneEdit() {
		CursesScreen screen = new( 80, 24 );
		using WidgetHost host = new( screen, WidgetTheme.Default );
		using EditorShell shell = new( host );
		string text = new( 'x', 1_000_000 );

		shell.Process( WidgetInput.Paste( text ), TimeSpan.Zero );
		screen.Resize( 4, 3 ); shell.Arrange(); host.Render();

		Assert.Equal( text.Length, shell.Document.Length );
		Assert.Equal( "1", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( "x", host.Screen.StandardWindow.GetCell( 2, 2 ).Content );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ),
			TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( 0, shell.Document.Length );
	}

	[Fact]
	public void RepeatedIndependentEditsUndoAndRedoWithoutLosingTheCheckpoint() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
		using EditorShell shell = new( host );
		for ( int index = 0; index < 64; index++ )
			shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromSeconds( index + 1 ) );
		Assert.Equal( 64, shell.Document.Length );

		for ( int index = 0; index < 64; index++ )
			shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'z' ) ),
				TimeSpan.FromSeconds( 65 + index ) );
		Assert.Equal( 0, shell.Document.Length );
		Assert.False( shell.Document.IsModified );

		for ( int index = 0; index < 64; index++ )
			shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'y' ) ),
				TimeSpan.FromSeconds( 129 + index ) );
		Assert.Equal( 64, shell.Document.Length );
		Assert.True( shell.Document.IsModified );
	}

	private static WidgetTheme PresentationTheme() {
		CursesStyle text = new( CursesColor.Indexed( 7 ), CursesColor.Indexed( 0 ) );
		CursesStyle current = new( CursesColor.Indexed( 7 ), CursesColor.Indexed( 4 ) );
		CursesStyle gutter = new( CursesColor.Indexed( 3 ), CursesColor.Indexed( 0 ) );
		CursesStyle currentGutter = new( CursesColor.Indexed( 15 ), CursesColor.Indexed( 4 ) );
		return WidgetTheme.Default with { Parts = WidgetStyles.Empty
			.With( WidgetStyleRole.EditorSurface, text ).With( WidgetStyleRole.EditorText, text )
			.With( WidgetStyleRole.EditorCurrentLine, current ).With( WidgetStyleRole.EditorGutter, gutter )
			.With( WidgetStyleRole.EditorGutterCurrentLine, currentGutter ) };
	}
}
