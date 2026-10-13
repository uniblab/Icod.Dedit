/*
	Icod.Dedit.Tests
	Checks explicit external-file decisions at the widget boundary.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERMERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Dedit.Tests;

using Icod.DCurses;
using Icod.Dedit.Documents;
using Icod.Widgets;
using Xunit;

public sealed class ExternalChangeCoordinatorTests {
	[Fact]
	public async Task KeepRetainsDirtyTextAndDoesNotPromptAgainUntilDiskChangesAgain() {
		string directory = Directory.CreateTempSubdirectory( "dedit-external-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 60, 18 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			entry.Shell.SetIndentationWidth( 2 );
			entry.Shell.Process( WidgetInput.Paste( "draft " ), TimeSpan.Zero );
			await File.WriteAllTextAsync( path, "external" );
			ExternalChangeCoordinator coordinator = new( host, manager, files );
			Assert.Equal( ExternalDecisionResult.DecisionRequired, await coordinator.CheckAsync( entry, CancellationToken.None ) );
			WidgetDialogCompletedAction kept = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.No, kept.Result.Outcome );
			Assert.Equal( ExternalDecisionResult.Kept, await coordinator.CompleteAsync( kept, CancellationToken.None ) );
			Assert.Equal( "draft original", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
			Assert.Equal( ExternalDecisionResult.Unchanged, await coordinator.CheckAsync( entry, CancellationToken.None ) );
			await Assert.ThrowsAsync<IOException>( () => manager.SaveAsync( entry, files, CancellationToken.None ) );
			await File.WriteAllTextAsync( path, "external again" );
			Assert.Equal( ExternalDecisionResult.DecisionRequired, await coordinator.CheckAsync( entry, CancellationToken.None ) );
			Assert.Equal( ExternalDecisionResult.Unrelated, await coordinator.CompleteAsync( kept, CancellationToken.None ) );
			WidgetDialogCompletedAction canceled = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
			Assert.Equal( ExternalDecisionResult.Canceled, await coordinator.CompleteAsync( canceled, CancellationToken.None ) );
			Assert.True( entry.Shell.Document.IsModified );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task ConfirmedReloadDiscardsDirtyTextAndMakesDiskVersionTheCheckpoint() {
		string directory = Directory.CreateTempSubdirectory( "dedit-external-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 60, 18 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			entry.Shell.Process( WidgetInput.Paste( "draft " ), TimeSpan.Zero );
			await File.WriteAllTextAsync( path, "disk" );
			ExternalChangeCoordinator coordinator = new( host, manager, files );
			Assert.Equal( ExternalDecisionResult.DecisionRequired, await coordinator.CheckAsync( entry, CancellationToken.None ) );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.Zero );
			WidgetDialogCompletedAction reload = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.Yes, reload.Result.Outcome );
			Assert.Equal( ExternalDecisionResult.Reloaded, await coordinator.CompleteAsync( reload, CancellationToken.None ) );
			Assert.Same( entry, manager.Active );
			Assert.Equal( "disk", entry.Shell.Document.Read( 0, 4 ) );
			Assert.False( entry.Shell.Document.IsModified );
			Assert.Equal( 2, entry.Shell.IndentationWidth );
			Assert.Equal( ExternalDecisionResult.Unchanged, await coordinator.CheckAsync( entry, CancellationToken.None ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task MissingExternalFileReportsConflictWithoutReloading() {
		string directory = Directory.CreateTempSubdirectory( "dedit-external-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 60, 18 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			File.Delete( path );
			ExternalChangeCoordinator coordinator = new( host, manager, files );
			Assert.Equal( ExternalDecisionResult.Missing, await coordinator.CheckAsync( entry, CancellationToken.None ) );
			Assert.Equal( "original", entry.Shell.Document.Read( 0, 8 ) );
		} finally { Directory.Delete( directory, true ); }
	}
}
