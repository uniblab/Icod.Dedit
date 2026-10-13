/*
	Icod.Dedit.Tests
	Exercises the Save As picker and document file transition together.
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

public sealed class SaveAsPickerTests {
	[Fact]
	public async Task AcceptedNewTargetSavesUntitledButEscapeLeavesItDirty() {
		string directory = Directory.CreateTempSubdirectory( "dedit-picker-" ).FullName;
		try {
			using WidgetHost host = new( new CursesScreen( 60, 18 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.OpenUntitled();
			entry.Shell.Process( WidgetInput.Paste( "draft"), TimeSpan.FromMilliseconds( 1 ) );
			DocumentFileService files = new( directory );
			DeditSaveTargetProvider provider = new();
			WidgetSaveTargetPicker canceled = new( "first-save", new LocalWidgetFileSystemProvider(), provider, directory, "draft.txt" );
			host.ShowModal( canceled );
			WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
				host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
			Assert.Equal( WidgetDialogOutcome.Cancel, cancel.Result.Outcome );
			Assert.Null( entry.FullPath ); Assert.True( entry.Shell.Document.IsModified );
			Assert.False( File.Exists( Path.Combine( directory, "draft.txt" ) ) );
			WidgetSaveTargetPicker accepted = new( "second-save", new LocalWidgetFileSystemProvider(), provider, directory, "draft.txt" );
			host.ShowModal( accepted );
			WidgetSaveTargetRequest request = accepted.RequestAccept();
			WidgetSaveTargetObservation observation = await provider.InspectTargetAsync( request.Directory, request.FileName, CancellationToken.None );
			Assert.True( accepted.ApplyInspectionResult( new( request, observation ) ) );
			WidgetDialogCompletedAction complete = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) );
			WidgetSaveTargetSelection selection = Assert.IsType<WidgetSaveTargetSelection>( complete.Result.Value );
			await manager.SaveAsNewAsync( entry, files, selection.TargetPath, CancellationToken.None );
			Assert.Equal( "draft", await File.ReadAllTextAsync( selection.TargetPath ) );
			Assert.False( entry.Shell.Document.IsModified );
			Assert.Equal( selection.TargetPath, entry.FullPath );
		} finally { Directory.Delete( directory, true ); }
	}
}
