/*
	Icod.Dedit.Tests
	Exercises opening selected files into independent tabs.
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

public sealed class OpenPickerTests {
	[Fact]
	public async Task AcceptedMultipleFilesOpenInSelectionOrderWithoutReplacingExistingTab() {
		string directory = Directory.CreateTempSubdirectory( "dedit-open-picker-" ).FullName;
		try {
			string firstPath = Path.Combine( directory, "first.txt" );
			string secondPath = Path.Combine( directory, "second.txt" );
			await File.WriteAllTextAsync( firstPath, "first" );
			await File.WriteAllTextAsync( secondPath, "second" );
			using WidgetHost host = new( new CursesScreen( 60, 18 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry draft = manager.OpenUntitled();
			draft.Shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.Zero );
			LocalWidgetFileSystemProvider provider = new();
			WidgetFilePicker picker = new( "open-files", provider, directory, allowMultiple: true, [ WidgetFileFilter.AllFiles ] );
			host.ShowModal( picker );
			WidgetBrowserRequest request = picker.RequestDirectory( directory );
			Assert.True( picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( provider, request, CancellationToken.None ) ) );
			picker.ToggleFile( firstPath );
			picker.ToggleFile( secondPath );
			WidgetPickerValidationRequest validation = picker.RequestAccept();
			Assert.True( picker.ApplyValidationResult( await WidgetPickerValidationLoader.ValidateAsync( provider, validation, CancellationToken.None ) ) );
			WidgetDialogCompletedAction selected = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) );
			IReadOnlyList<string> paths = Assert.IsAssignableFrom<IReadOnlyList<string>>( selected.Result.Value );
			DocumentFileService files = new( directory );
			foreach ( string path in paths ) manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			Assert.Equal( new[] { draft.Key, firstPath, secondPath }, manager.Tabs.Select( tab =>
				manager.Find( tab.Key ) == draft ? draft.Key : manager.Find( tab.Key )!.FullPath! ).ToArray() );
			Assert.True( draft.Shell.Document.IsModified );
			Assert.Equal( "second", manager.Active?.Shell.Document.Read( 0, 6 ) );
		} finally { Directory.Delete( directory, true ); }
	}
}
