/*
	Icod.Dedit.Tests
	Checks ordered save and close operations across documents.
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
using Icod.Widgets;
using Xunit;

public sealed class DocumentBatchCoordinatorTests {
	[Fact]
	public async Task CloseAllCleanDocumentsLeavesNoActiveDocument() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		manager.OpenUntitled();
		manager.OpenUntitled();
		DocumentFileService files = new( Path.GetTempPath() );
		DocumentBatchCoordinator batch = new( manager, files, new CloseCoordinator( host, manager, files ) );

		Assert.Equal( BatchProgress.Completed, await batch.StartAsync( BatchOperation.CloseAll, CancellationToken.None ) );
		Assert.Empty( manager.Tabs );
		Assert.Null( manager.Active );
	}

	[Fact]
	public async Task ExitWithNoDocumentsIsImmediatelyReady() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentFileService files = new( Path.GetTempPath() );
		DocumentBatchCoordinator batch = new( manager, files, new CloseCoordinator( host, manager, files ) );

		Assert.Equal( BatchProgress.ExitReady, await batch.StartAsync( BatchOperation.Exit, CancellationToken.None ) );
		Assert.False( batch.IsActive );
		Assert.Empty( manager.Tabs );
	}

	[Fact]
	public async Task SaveAllCancellationAtUntitledKeepsPriorSaveAndDoesNotTouchLaterTab() {
		string directory = Directory.CreateTempSubdirectory( "dedit-batch-" ).FullName;
		try {
			DocumentFileService files = new( directory );
			string firstPath = Path.Combine( directory, "first.txt" );
			string lastPath = Path.Combine( directory, "last.txt" );
			await File.WriteAllTextAsync( firstPath, "first" );
			await File.WriteAllTextAsync( lastPath, "last" );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry first = manager.Open( await files.OpenAsync( firstPath, CancellationToken.None ) );
			first.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.Zero );
			DocumentEntry middle = manager.OpenUntitled();
			middle.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.Zero );
			DocumentEntry last = manager.Open( await files.OpenAsync( lastPath, CancellationToken.None ) );
			last.Shell.Process( WidgetInput.Paste( "?" ), TimeSpan.Zero );
			DocumentBatchCoordinator batch = new( manager, files, new CloseCoordinator( host, manager, files ) );
			Assert.Equal( BatchProgress.SaveAsRequired, await batch.StartAsync( BatchOperation.SaveAll, CancellationToken.None ) );
			Assert.Equal( middle.Key, batch.SaveAsKey );
			Assert.Equal( "!first", await File.ReadAllTextAsync( firstPath ) );
			Assert.False( first.Shell.Document.IsModified );
			Assert.Equal( BatchProgress.Stopped, await batch.CompleteSaveAsAsync( false, CancellationToken.None ) );
			Assert.Equal( "last", await File.ReadAllTextAsync( lastPath ) );
			Assert.True( last.Shell.Document.IsModified );
			Assert.Equal( 3, manager.Tabs.Count );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task CloseAllCancellationAtSecondDirtyTabLeavesItAndLaterTabsOpen() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry first = manager.OpenUntitled();
		DocumentEntry middle = manager.OpenUntitled();
		middle.Shell.Process( WidgetInput.Paste( "keep" ), TimeSpan.Zero );
		DocumentEntry last = manager.OpenUntitled();
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );
		DocumentBatchCoordinator batch = new( manager, new DocumentFileService( Path.GetTempPath() ), close );
		Assert.Equal( BatchProgress.DecisionRequired, await batch.StartAsync( BatchOperation.CloseAll, CancellationToken.None ) );
		Assert.Null( manager.Find( first.Key ) );
		WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( BatchProgress.Stopped, await batch.CompleteDecisionAsync( cancel, CancellationToken.None ) );
		Assert.Same( middle, manager.Find( middle.Key ) );
		Assert.Same( last, manager.Find( last.Key ) );
		Assert.True( middle.Shell.Document.IsModified );
	}


	[Fact]
	public async Task SaveAllConflictKeepsPriorSaveAndStopsTheOperation() {
		string directory = Directory.CreateTempSubdirectory( "dedit-batch-" ).FullName;
		try {
			string firstPath = Path.Combine( directory, "first.txt" );
			string secondPath = Path.Combine( directory, "second.txt" );
			await File.WriteAllTextAsync( firstPath, "first" );
			await File.WriteAllTextAsync( secondPath, "second" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry first = manager.Open( await files.OpenAsync( firstPath, CancellationToken.None ) );
			first.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.Zero );
			DocumentEntry second = manager.Open( await files.OpenAsync( secondPath, CancellationToken.None ) );
			second.Shell.Process( WidgetInput.Paste( "?" ), TimeSpan.Zero );
			await File.WriteAllTextAsync( secondPath, "external" );
			DocumentBatchCoordinator batch = new( manager, files, new CloseCoordinator( host, manager, files ) );
			await Assert.ThrowsAsync<IOException>( () => batch.StartAsync( BatchOperation.SaveAll, CancellationToken.None ) );
			Assert.False( batch.IsActive );
			Assert.Equal( "!first", await File.ReadAllTextAsync( firstPath ) );
			Assert.False( first.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( secondPath ) );
			Assert.True( second.Shell.Document.IsModified );
		} finally { Directory.Delete( directory, true ); }
	}


	[Fact]
	public async Task ExitSaveConflictKeepsTabAndResetsTheOperation() {
		string directory = Directory.CreateTempSubdirectory( "dedit-batch-" ).FullName;
		try {
			string path = Path.Combine( directory, "opened.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry opened = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			opened.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.Zero );
			await File.WriteAllTextAsync( path, "external" );
			DocumentBatchCoordinator batch = new( manager, files, new CloseCoordinator( host, manager, files ) );
			Assert.Equal( BatchProgress.DecisionRequired, await batch.StartAsync( BatchOperation.Exit, CancellationToken.None ) );
			WidgetDialogCompletedAction save = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) ), TimeSpan.Zero ).Actions ) );
			await Assert.ThrowsAsync<IOException>( () => batch.CompleteDecisionAsync( save, CancellationToken.None ) );
			Assert.False( batch.IsActive );
			Assert.Same( opened, manager.Find( opened.Key ) );
			Assert.True( opened.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task ExitWaitsForSavedDocumentBeforeExiting() {
		string directory = Directory.CreateTempSubdirectory( "dedit-batch-" ).FullName;
		try {
			string path = Path.Combine( directory, "first.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry first = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			first.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.Zero );
			CloseCoordinator close = new( host, manager, files );
			DocumentBatchCoordinator batch = new( manager, files, close );
			Assert.Equal( BatchProgress.DecisionRequired, await batch.StartAsync( BatchOperation.Exit, CancellationToken.None ) );
			WidgetDialogCompletedAction save = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) ), TimeSpan.Zero ).Actions ) );
			Assert.Equal( BatchProgress.ExitReady, await batch.CompleteDecisionAsync( save, CancellationToken.None ) );
			Assert.Equal( "!before", await File.ReadAllTextAsync( path ) );
			Assert.Null( manager.Find( first.Key ) );
		} finally { Directory.Delete( directory, true ); }
	}
}
