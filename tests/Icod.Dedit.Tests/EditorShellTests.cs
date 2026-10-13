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
		using WidgetHost host = new( new CursesScreen( 8, 32 ), WidgetTheme.Default );
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
		using WidgetHost host = new( new CursesScreen( 3, 7 ), WidgetTheme.Default );
		using EditorShell shell = new( host ); shell.Arrange();
		shell.Process( WidgetInput.Paste( "長い行\nsecond" ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "長い行\nsecond", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		host.Render();
		Assert.True( shell.Editor.Bounds.Rows <= host.Screen.Rows );
	}
}
