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



	[Fact]
	public async Task SaveAsExistingUpdatesIdentityOnlyAfterReplacingApprovedTarget() {
		string directory = Directory.CreateTempSubdirectory( "dedit-overwrite-" ).FullName;
		try {
			string path = Path.Combine( directory, "existing.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry draft = manager.OpenUntitled();
			draft.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.Zero );
			var observed = await files.CaptureExistingAsync( path, CancellationToken.None );
			await manager.SaveAsExistingAsync( draft, files, observed, CancellationToken.None );
			Assert.Equal( "draft", await File.ReadAllTextAsync( path ) );
			Assert.Equal( path, draft.FullPath );
			Assert.Equal( observed.Identity, draft.Identity );
			Assert.Equal( "existing.txt", draft.Caption );
			Assert.False( draft.Shell.Document.IsModified );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveAsExistingRejectsTargetChangedDuringConfirmation() {
		string directory = Directory.CreateTempSubdirectory( "dedit-overwrite-" ).FullName;
		try {
			string path = Path.Combine( directory, "existing.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry draft = manager.OpenUntitled();
			draft.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.Zero );
			var observed = await files.CaptureExistingAsync( path, CancellationToken.None );
			await File.WriteAllTextAsync( path, "external" );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsExistingAsync( draft, files, observed, CancellationToken.None ) );
			Assert.Null( draft.FullPath );
			Assert.True( draft.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveAsExistingRefusesAnotherOpenTab() {
		string directory = Directory.CreateTempSubdirectory( "dedit-overwrite-" ).FullName;
		try {
			string path = Path.Combine( directory, "owned.txt" );
			await File.WriteAllTextAsync( path, "owned" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry owner = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			DocumentEntry draft = manager.OpenUntitled();
			draft.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.Zero );
			var observed = await files.CaptureExistingAsync( path, CancellationToken.None );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsExistingAsync( draft, files, observed, CancellationToken.None ) );
			Assert.Same( owner, manager.Find( owner.Key ) );
			Assert.Null( draft.FullPath );
			Assert.True( draft.Shell.Document.IsModified );
			Assert.Equal( "owned", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveAsToOwnPathDoesNotApproveAnExternalChange() {
		string directory = Directory.CreateTempSubdirectory( "dedit-overwrite-" ).FullName;
		try {
			string path = Path.Combine( directory, "opened.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry opened = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			opened.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.Zero );
			await File.WriteAllTextAsync( path, "external" );
			var observed = await files.CaptureExistingAsync( path, CancellationToken.None );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsExistingAsync( opened, files, observed, CancellationToken.None ) );
			Assert.Equal( path, opened.FullPath );
			Assert.True( opened.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public void CloseRefusesDirtyTabUntilDiscardAndKeepsOtherTabsIntact() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry dirty = manager.OpenUntitled();
		dirty.Shell.Process( WidgetInput.Paste( "keep" ), TimeSpan.FromMilliseconds( 1 ) );
		DocumentEntry clean = manager.OpenUntitled();
		Assert.False( manager.Close( dirty.Key ) );
		Assert.Equal( 2, manager.Tabs.Count );
		Assert.True( dirty.Shell.Document.IsModified );
		Assert.Same( clean, manager.Active );
		Assert.True( manager.Close( dirty.Key, discardModified: true ) );
		Assert.Single( manager.Tabs );
		Assert.Same( clean, manager.Active );
		Assert.True( manager.Close( clean.Key ) );
		Assert.Single( manager.Tabs );
		Assert.NotEqual( clean.Key, manager.Active?.Key );
		Assert.StartsWith( "Untitled", manager.Active?.Caption );
	}

	[Fact]
	public void ClosingActiveTabSelectsNextNeighborWithoutReusingItsKey() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry first = manager.OpenUntitled();
		DocumentEntry middle = manager.OpenUntitled();
		DocumentEntry last = manager.OpenUntitled();
		manager.Activate( middle.Key );
		Assert.True( manager.Close( middle.Key ) );
		Assert.Same( last, manager.Active );
		Assert.Equal( [ first.Key, last.Key ], manager.Tabs.Select( item => item.Key ).ToArray() );
		Assert.False( manager.Close( "unknown" ) );
	}


	[Fact]
	public async Task ExternalChangeDetectionFindsModifiedAndMissingFilesWithoutReplacingEditorText() {
		string directory = Directory.CreateTempSubdirectory( "dedit-reload-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "initial" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			Assert.Null( await manager.CheckExternalChangeAsync( entry, files, CancellationToken.None ) );
			await File.WriteAllTextAsync( path, "external" );
			Assert.Equal( ExternalChangeKind.Modified,
				( await manager.CheckExternalChangeAsync( entry, files, CancellationToken.None ) )?.Kind );
			Assert.Equal( "initial", entry.Shell.Document.Read( 0, 7 ) );
			File.Delete( path );
			Assert.Equal( ExternalChangeKind.Missing,
				( await manager.CheckExternalChangeAsync( entry, files, CancellationToken.None ) )?.Kind );
			Assert.Equal( "initial", entry.Shell.Document.Read( 0, 7 ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task ConfirmedReloadReplacesDiskTextAndEncodingButPreservesTabKey() {
		string directory = Directory.CreateTempSubdirectory( "dedit-reload-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "initial" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			string key = entry.Key;
			entry.Shell.Process( WidgetInput.Paste( "draft " ), TimeSpan.Zero );
			await File.WriteAllBytesAsync( path, TextFileCodec.Encode( "external\r\n", TextFileEncoding.Utf16LittleEndian ) );
			await manager.ReloadAsync( entry, files, discardModified: true, CancellationToken.None );
			Assert.Equal( key, entry.Key );
			Assert.Same( entry, manager.Active );
			Assert.Equal( "external\r\n", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
			Assert.Equal( TextFileEncoding.Utf16LittleEndian, entry.Encoding );
			Assert.False( entry.Shell.Document.IsModified );
			Assert.Null( await manager.CheckExternalChangeAsync( entry, files, CancellationToken.None ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task ReloadWithoutExplicitDiscardKeepsDirtyBufferAndOriginalCheckpoint() {
		string directory = Directory.CreateTempSubdirectory( "dedit-reload-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "initial" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			entry.Shell.Process( WidgetInput.Paste( "draft " ), TimeSpan.Zero );
			await File.WriteAllTextAsync( path, "external" );
			await Assert.ThrowsAsync<InvalidOperationException>( () =>
				manager.ReloadAsync( entry, files, discardModified: false, CancellationToken.None ) );
			Assert.Equal( "draft initial", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
			Assert.True( entry.Shell.Document.IsModified );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsync( entry, files, CancellationToken.None ) );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task InvalidExternalEncodingDoesNotDiscardDirtyBufferEvenAfterReloadConfirmation() {
		string directory = Directory.CreateTempSubdirectory( "dedit-reload-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "initial" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			entry.Shell.Process( WidgetInput.Paste( "draft " ), TimeSpan.Zero );
			await File.WriteAllBytesAsync( path, [ 0xFF ] );
			await Assert.ThrowsAsync<InvalidDataException>( () =>
				manager.ReloadAsync( entry, files, discardModified: true, CancellationToken.None ) );
			Assert.Equal( "draft initial", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
			Assert.True( entry.Shell.Document.IsModified );
			Assert.Equal( TextFileEncoding.Utf8, entry.Encoding );
		} finally { Directory.Delete( directory, true ); }
	}

	private static LoadedTextFile Loaded( string path, string text ) => new( path, new DecodedText( text, TextFileEncoding.Utf8 ), [] );
}
