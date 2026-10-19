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

using System.Text;
using Icod.DCurses;
using Icod.Dedit.Documents;
using Icod.Widgets;
using Xunit;

public sealed class CloseCoordinatorTests {
	[Fact]
	public void CleanDocumentClosesImmediatelyWithoutShowingAModal() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry clean = manager.OpenUntitled();
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );

		Assert.True( close.Request( clean.Key ) );
		Assert.Null( manager.Find( clean.Key ) );
		Assert.Empty( manager.Tabs );
		Assert.Null( manager.Active );
		Assert.Null( host.GetCaretIntent() );
		Assert.Equal( 0, host.ModalDepth );
	}

	[Theory]
	[InlineData( 100, 30, 12, 27, 5, 45 )]
	[InlineData( 20, 3, 0, 0, 3, 20 )]
	public void DirtyPromptUsesDeditSpecifiedCompactBounds( int columns, int rows,
		int expectedRow, int expectedColumn, int expectedRows, int expectedColumns ) {
		using WidgetHost host = new( new CursesScreen( columns, rows ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry dirty = manager.OpenUntitled();
		dirty.Shell.Process( WidgetInput.Paste( "dirty" ), TimeSpan.Zero );
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );

		Assert.False( close.Request( dirty.Key ) );

		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );
		Assert.Equal( new CursesRectangle( expectedRow, expectedColumn, expectedRows, expectedColumns ), dialog.Bounds );
	}

	[Fact]
	public void DirtyPromptRecentersAndClampsAfterResize() {
		CursesScreen screen = new( 100, 30 );
		using WidgetHost host = new( screen, WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry dirty = manager.OpenUntitled();
		dirty.Shell.Process( WidgetInput.Paste( "dirty" ), TimeSpan.Zero );
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );
		Assert.False( close.Request( dirty.Key ) );
		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );
		screen.Resize( 60, 10 );
		close.Arrange();
		Assert.Equal( new CursesRectangle( 2, 7, 5, 45 ), dialog.Bounds );

		screen.Resize( 30, 4 );
		close.Arrange();
		Assert.Equal( new CursesRectangle( 0, 0, 4, 30 ), dialog.Bounds );
	}

	[Fact]
	public void UnchangedLayoutBetweenPointerPressAndReleasePreservesClick() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry dirty = manager.OpenUntitled();
		dirty.Shell.Process( WidgetInput.Paste( "dirty" ), TimeSpan.Zero );
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );
		Assert.False( close.Request( dirty.Key ) );
		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );
		host.Render();
		int row = dialog.Bounds.BottomExclusive - 2;
		int column = dialog.Bounds.Column + 23;

		Assert.Empty( host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press,
			CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero ).Actions );
		close.Arrange();
		host.Render();
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Release,
				CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Cancel, completed.Result.Outcome );
	}

	[Theory]
	[InlineData( "enter" )]
	[InlineData( "escape" )]
	[InlineData( "c" )]
	public async Task DefaultEnterEscapeAndCMnemonicRetainDirtyTab( string input ) {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry dirty = manager.OpenUntitled();
		dirty.Shell.Process( WidgetInput.Paste( "keep" ), TimeSpan.Zero );
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );
		Assert.False( close.Request( dirty.Key ) );

		WidgetInput decision = input switch {
			"enter" => WidgetInput.Key( WidgetKey.Enter ),
			"escape" => WidgetInput.Key( WidgetKey.Escape ),
			_ => WidgetInput.Key( WidgetKey.Character, character: new Rune( 'c' ) )
		};
		WidgetDialogCompletedAction canceled = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( decision, TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Cancel, canceled.Result.Outcome );
		Assert.Equal( CloseResolution.Kept, await close.CompleteAsync( canceled, CancellationToken.None ) );
		Assert.Same( dirty, manager.Find( dirty.Key ) );
		Assert.True( dirty.Shell.Document.IsModified );
	}

	[Fact]
	public async Task NMnemonicDiscardsDirtyTab() {
		string directory = Directory.CreateTempSubdirectory( "dedit-close-" ).FullName;
		try {
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry dirty = manager.OpenUntitled();
			dirty.Shell.Process( WidgetInput.Paste( "keep" ), TimeSpan.FromMilliseconds( 1 ) );
			CloseCoordinator close = new( host, manager, new DocumentFileService( directory ) );
			Assert.False( close.Request( dirty.Key ) );
			WidgetDialogCompletedAction discarded = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'n' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.No, discarded.Result.Outcome );
			Assert.Equal( CloseResolution.Closed, await close.CompleteAsync( discarded, CancellationToken.None ) );
			Assert.Empty( manager.Tabs );
			Assert.Null( manager.Active );
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
			WidgetDialogCompletedAction save = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.Yes, save.Result.Outcome );
			Assert.Equal( CloseResolution.Closed, await close.CompleteAsync( save, CancellationToken.None ) );
			Assert.Equal( "!before", await File.ReadAllTextAsync( path ) );
			Assert.Empty( manager.Tabs );
			Assert.Null( manager.Active );
			DocumentEntry untitled = manager.OpenUntitled();
			untitled.Shell.Process( WidgetInput.Paste( "new" ), TimeSpan.FromMilliseconds( 5 ) );
			Assert.False( close.Request( untitled.Key ) );
			WidgetDialogCompletedAction needPath = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) ), TimeSpan.FromMilliseconds( 6 ) ).Actions ) );
			Assert.Equal( CloseResolution.SaveAsRequired, await close.CompleteAsync( needPath, CancellationToken.None ) );
			Assert.Equal( untitled.Key, manager.Active?.Key );
		} finally { Directory.Delete( directory, true ); }
	}


	[Fact]
	public async Task LateDecisionFromPreviousTabCannotCloseCurrentTab() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry first = manager.OpenUntitled();
		first.Shell.Process( WidgetInput.Paste( "first" ), TimeSpan.Zero );
		DocumentEntry second = manager.OpenUntitled();
		second.Shell.Process( WidgetInput.Paste( "second" ), TimeSpan.Zero );
		CloseCoordinator close = new( host, manager, new DocumentFileService( Path.GetTempPath() ) );
		Assert.False( close.Request( first.Key ) );
		WidgetDialogCompletedAction oldCancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( CloseResolution.Kept, await close.CompleteAsync( oldCancel, CancellationToken.None ) );
		Assert.False( close.Request( second.Key ) );
		Assert.Equal( CloseResolution.Unrelated, await close.CompleteAsync( oldCancel, CancellationToken.None ) );
		Assert.Same( second, manager.Find( second.Key ) );
		WidgetDialogCompletedAction currentCancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( CloseResolution.Kept, await close.CompleteAsync( currentCancel, CancellationToken.None ) );
		Assert.True( second.Shell.Document.IsModified );
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
			WidgetDialogCompletedAction save = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
			await Assert.ThrowsAsync<IOException>( () => close.CompleteAsync( save, CancellationToken.None ) );
			Assert.Equal( opened.Key, manager.Active?.Key );
			Assert.True( opened.Shell.Document.IsModified );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Theory]
	[InlineData( "yes", false, WidgetDialogOutcome.Yes, true )]
	[InlineData( "yes", true, WidgetDialogOutcome.Yes, true )]
	[InlineData( "no", false, WidgetDialogOutcome.No, true )]
	[InlineData( "no", true, WidgetDialogOutcome.No, true )]
	[InlineData( "cancel", false, WidgetDialogOutcome.Cancel, false )]
	[InlineData( "cancel", true, WidgetDialogOutcome.Cancel, false )]
	public async Task PointerAndMnemonicActivationShareClosePolicy( string choice, bool pointer,
		WidgetDialogOutcome outcome, bool closes ) {
		string directory = Directory.CreateTempSubdirectory( "dedit-close-parity-" ).FullName;
		try {
			string path = Path.Combine( directory, "opened.txt" );
			await File.WriteAllTextAsync( path, "before" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry dirty = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			dirty.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.Zero );
			CloseCoordinator close = new( host, manager, files );
			Assert.False( close.Request( dirty.Key ) );

			WidgetDialogCompletedAction completed = pointer
				? ClickDecision( host, choice )
				: Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process(
					WidgetInput.Key( WidgetKey.Character, character: new Rune( choice[ 0 ] ) ), TimeSpan.Zero ).Actions ) );

			Assert.Equal( outcome, completed.Result.Outcome );
			Assert.Equal( closes ? CloseResolution.Closed : CloseResolution.Kept,
				await close.CompleteAsync( completed, CancellationToken.None ) );
			Assert.Equal( !closes, manager.Find( dirty.Key ) is not null );
			Assert.Equal( choice == "yes" ? "!before" : "before", await File.ReadAllTextAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	private static WidgetDialogCompletedAction ClickDecision( WidgetHost host, string choice ) {
		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );
		host.Render();
		int row = dialog.Bounds.BottomExclusive - 2;
		int column = dialog.Bounds.Column + ( choice switch { "yes" => 2, "no" => 9, _ => 23 } );
		Assert.Empty( host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press,
			CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero ).Actions );
		return Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process(
			WidgetInput.PointerInput( new( CursesMouseAction.Release, CursesMouseButton.Primary, column, row ) ),
			TimeSpan.Zero ).Actions ) );
	}
}
