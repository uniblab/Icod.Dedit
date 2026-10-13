/*
	Icod.Dedit.Tests
	Exercises independent document views and stable tab identities.
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
using Icod.Dedit.Documents;
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class DocumentManagerTests {
	[Fact]
	public void SwitchingTabsPreservesIndependentTextHistoryCaretAndStableKeys() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry first = manager.OpenUntitled();
		first.Shell.Process( WidgetInput.Paste( "first" ), TimeSpan.FromMilliseconds( 1 ) );
		first.Shell.Editor.View.SetSelection( 2, 2 );
		DocumentEntry second = manager.Open( Loaded( Path.Combine( Path.GetTempPath(), "second.txt" ), "second" ) );
		Assert.NotEqual( first.Key, second.Key );
		Assert.Same( second, manager.Active );
		second.Shell.Process( WidgetInput.TextInput( new Rune( '!' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.True( manager.Activate( first.Key ) );
		Assert.Equal( "first", first.Shell.Document.Read( 0, 5 ) );
		Assert.Equal( 2, first.Shell.Editor.View.Caret );
		Assert.True( first.Shell.Document.IsModified );
		Assert.True( manager.Activate( second.Key ) );
		Assert.Equal( "!second", second.Shell.Document.Read( 0, 7 ) );
		Assert.True( second.Shell.Document.IsModified );
		Assert.Equal( second.Key, manager.Tabs[ 1 ].Key );
	}

	[Fact]
	public void DuplicatePathActivatesExistingTabAndDistinctBasenamesAreDisambiguated() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry a = manager.Open( Loaded( Path.Combine( Path.GetTempPath(), "one", "note.txt" ), "A" ) );
		DocumentEntry b = manager.Open( Loaded( Path.Combine( Path.GetTempPath(), "two", "note.txt" ), "B" ) );
		Assert.NotEqual( manager.Tabs[ 0 ].Caption, manager.Tabs[ 1 ].Caption );
		Assert.Same( a, manager.Open( Loaded( Path.Combine( Path.GetTempPath(), "one", "..", "one", "note.txt" ), "ignored" ) ) );
		Assert.Same( a, manager.Active );
		Assert.Equal( 2, manager.Tabs.Count );
		Assert.False( manager.Activate( "unknown" ) );
		Assert.Same( a, manager.Active );
		Assert.Equal( "B", b.Shell.Document.Read( 0, 1 ) );
	}

	[Fact]
	public void TabNavigationKeepsStripFocusedForRepeatedArrows() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetTabStrip tabs = new( "tabs" ); host.Attach( tabs ); tabs.Arrange( new CursesRectangle( 1, 0, 1, 50 ) );
		using DocumentManager manager = new( host );
		DocumentEntry first = manager.OpenUntitled(); DocumentEntry second = manager.OpenUntitled();
		host.Focus( tabs );
		Assert.True( manager.Activate( first.Key, focusEditor: false ) );
		Assert.Same( tabs, host.FocusedWidget );
		Assert.True( manager.Activate( second.Key, focusEditor: false ) );
		Assert.Same( tabs, host.FocusedWidget );
	}

	[Fact]
	public void GoToFileThenRightArrowActivatesSecondDocument() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); WidgetTabStrip tabs = new( "workspace-tabs" );
		host.Attach( menu ); host.Attach( tabs ); menu.Arrange( host.Screen.Bounds );
		tabs.Arrange( new CursesRectangle( 1, 0, 1, 50 ) );
		using DocumentManager manager = new( host );
		DocumentEntry first = manager.OpenUntitled(); DocumentEntry second = manager.OpenUntitled();
		manager.Activate( first.Key ); tabs.SetTabs( manager.Tabs, first.Key );
		Assert.Equal( DeditMenu.GoToFile, Assert.IsType<WidgetInvokedAction>( Assert.Single(
			first.Shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'p' ) ), TimeSpan.FromMilliseconds( 1 ) ) ) ).CommandId );
		host.Focus( tabs );
		WidgetTabActivationRequestedAction activation = Assert.IsType<WidgetTabActivationRequestedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.Equal( second.Key, activation.TabKey );
		manager.Activate( activation.TabKey, focusEditor: false );
		Assert.Same( second, manager.Active );
	}

	[Fact]
	public void PathCaptionEscapesTerminalControlCharacters() {
		Assert.Equal( "bad�name.txt", DocumentManager.SanitizeDisplay( "bad\nname.txt" ) );
	}


	[Fact]
	public async Task SaveUpdatesDiskAndCheckpointThenFurtherEditIsDirty() {
		string directory = Directory.CreateTempSubdirectory( "dedit-document-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			entry.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.FromMilliseconds( 1 ) );
			Assert.True( entry.Shell.Document.IsModified );
			await manager.SaveAsync( entry, files, CancellationToken.None );
			Assert.Equal( "!before", await File.ReadAllTextAsync( path ) );
			Assert.False( entry.Shell.Document.IsModified );
			entry.Shell.Process( WidgetInput.Paste( "?" ), TimeSpan.FromMilliseconds( 2 ) );
			Assert.True( entry.Shell.Document.IsModified );
			await manager.SaveAsync( entry, files, CancellationToken.None );
			Assert.False( entry.Shell.Document.IsModified );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveConflictKeepsDirtyDocumentAndExternalBytes() {
		string directory = Directory.CreateTempSubdirectory( "dedit-document-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			entry.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.FromMilliseconds( 1 ) );
			await File.WriteAllTextAsync( path, "external" );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsync( entry, files, CancellationToken.None ) );
			Assert.True( entry.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}


	[Fact]
	public async Task SaveAsNewAssignsUntitledIdentityOnlyAfterSuccessfulWrite() {
		string directory = Directory.CreateTempSubdirectory( "dedit-save-as-" ).FullName;
		try {
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.OpenUntitled();
			entry.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.FromMilliseconds( 1 ) );
			await manager.SaveAsNewAsync( entry, files, "draft.txt", CancellationToken.None );
			Assert.Equal( "draft", await File.ReadAllTextAsync( Path.Combine( directory, "draft.txt" ) ) );
			Assert.Equal( Path.Combine( directory, "draft.txt" ), entry.FullPath );
			Assert.False( entry.Shell.Document.IsModified );
			Assert.Equal( "draft.txt", entry.Caption );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveAsNewCollisionAndExistingTargetRetainOriginalDocumentIdentity() {
		string directory = Directory.CreateTempSubdirectory( "dedit-save-as-" ).FullName;
		try {
			DocumentFileService files = new( directory );
			string ownedPath = Path.Combine( directory, "owned.txt" );
			await File.WriteAllTextAsync( ownedPath, "owned" );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			manager.Open( await files.OpenAsync( ownedPath, CancellationToken.None ) );
			DocumentEntry untitled = manager.OpenUntitled();
			untitled.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.FromMilliseconds( 1 ) );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsNewAsync( untitled, files, ownedPath, CancellationToken.None ) );
			Assert.Null( untitled.FullPath ); Assert.True( untitled.Shell.Document.IsModified );
			Assert.Equal( "owned", await File.ReadAllTextAsync( ownedPath ) );
		} finally { Directory.Delete( directory, true ); }
	}

	private static LoadedTextFile Loaded( string path, string text ) => new( path, new DecodedText( text, TextFileEncoding.Utf8 ), [] );
}
