/*
	Icod.Dedit.Tests
	Checks save, discard and cancel decisions for closing tabs.
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

using Icod.DCurses;
using Icod.Dedit.Documents;
using Icod.Widgets;
using Xunit;

public sealed class CloseCoordinatorTests {
	[Fact]
	public async Task CancelRetainsDirtyTabAndDontSaveRemovesIt() {
		string directory = Directory.CreateTempSubdirectory( "dedit-close-" ).FullName;
		try {
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry dirty = manager.OpenUntitled();
			dirty.Shell.Process( WidgetInput.Paste( "keep" ), TimeSpan.FromMilliseconds( 1 ) );
			CloseCoordinator close = new( host, manager, new DocumentFileService( directory ) );
			Assert.False( close.Request( dirty.Key ) );
			WidgetDialogCompletedAction canceled = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
			Assert.Equal( CloseResolution.Kept, await close.CompleteAsync( canceled, CancellationToken.None ) );
			Assert.Equal( dirty.Key, manager.Active?.Key );
			Assert.True( dirty.Shell.Document.IsModified );
			Assert.False( close.Request( dirty.Key ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 3 ) );
			WidgetDialogCompletedAction discarded = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 4 ) ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.DontSave, discarded.Result.Outcome );
			Assert.Equal( CloseResolution.Closed, await close.CompleteAsync( discarded, CancellationToken.None ) );
			Assert.NotEqual( dirty.Key, manager.Active?.Key );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveClosesOnlyAfterSuccessfulDiskWriteAndUntitledRequestsSaveAs() {
		string directory = Directory.CreateTempSubdirectory( "dedit-close-" ).FullName;
		try {
			string path = Path.Combine( directory, "opened.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry opened = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			opened.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.FromMilliseconds( 1 ) );
			CloseCoordinator close = new( host, manager, files );
			Assert.False( close.Request( opened.Key ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 2 ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 3 ) );
			WidgetDialogCompletedAction save = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 4 ) ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.Save, save.Result.Outcome );
			Assert.Equal( CloseResolution.Closed, await close.CompleteAsync( save, CancellationToken.None ) );
			Assert.Equal( "!before", await File.ReadAllTextAsync( path ) );
			DocumentEntry untitled = manager.Active!;
			untitled.Shell.Process( WidgetInput.Paste( "new" ), TimeSpan.FromMilliseconds( 5 ) );
			Assert.False( close.Request( untitled.Key ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 6 ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 7 ) );
			WidgetDialogCompletedAction needPath = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 8 ) ).Actions ) );
			Assert.Equal( CloseResolution.SaveAsRequired, await close.CompleteAsync( needPath, CancellationToken.None ) );
			Assert.Equal( untitled.Key, manager.Active?.Key );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveConflictRetainsDirtyTabAndExternalFile() {
		string directory = Directory.CreateTempSubdirectory( "dedit-close-" ).FullName;
		try {
			string path = Path.Combine( directory, "opened.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry opened = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			opened.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.FromMilliseconds( 1 ) );
			await File.WriteAllTextAsync( path, "external" );
			CloseCoordinator close = new( host, manager, files );
			Assert.False( close.Request( opened.Key ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 2 ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 3 ) );
			WidgetDialogCompletedAction save = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 4 ) ).Actions ) );
			await Assert.ThrowsAsync<IOException>( () => close.CompleteAsync( save, CancellationToken.None ) );
			Assert.Equal( opened.Key, manager.Active?.Key );
			Assert.True( opened.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}
}
