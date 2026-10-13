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
		using WidgetHost host = new( new CursesScreen( 7, 3 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); shell.Arrange();
		shell.Process( WidgetInput.Paste( "長い行\nsecond" ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "長い行\nsecond", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		host.Render();
		Assert.True( shell.Editor.Bounds.Rows <= host.Screen.Rows );
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
		Assert.Equal( "x", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.InRange( GC.GetAllocatedBytesForCurrentThread() - before, 0, 5_000_000 );
	}

	[Fact]
	public void FirstTypedCharacterAppearsAfterInitialRender() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); host.Render();
		shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromMilliseconds( 1 ) );
		host.Render();
		Assert.Equal( "x", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
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
			CursesMouseButton.Primary, 2, 2 ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( 1, shell.Editor.View.Caret );
		screen.Resize( 6, 4 ); shell.Arrange(); host.Render();
		shell.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press,
			CursesMouseButton.Primary, 0, 2 ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Equal( 0, shell.Editor.View.Caret );
		Assert.Equal( "界z", shell.Document.Read( 0, 2 ) );
	}
}
