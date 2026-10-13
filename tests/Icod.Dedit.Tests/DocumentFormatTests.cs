/*
	Icod.Dedit.Tests
	Checks save-affecting encoding metadata and snapshot checkpoints.
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
using Icod.Dedit.Presentation;
using System.Text;
using Icod.Widgets;
using Xunit;

public sealed class DocumentFormatTests {
	[Fact]
	public async Task EncodingChangeDirtiesCleanTabUntilExactFormatIsSaved() {
		string directory = Directory.CreateTempSubdirectory( "dedit-format-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "text" );
			DocumentFileService files = new( directory );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			Assert.False( entry.HasUnsavedChanges );
			manager.SetEncoding( entry, TextFileEncoding.Utf8Bom );
			Assert.True( entry.HasUnsavedChanges );
			Assert.True( Assert.Single( manager.Tabs ).IsModified );
			Assert.False( manager.Close( entry.Key ) );
			await manager.SaveAsync( entry, files, CancellationToken.None );
			Assert.Equal( TextFileCodec.Encode( "text", TextFileEncoding.Utf8Bom ), await File.ReadAllBytesAsync( path ) );
			Assert.False( entry.HasUnsavedChanges );
			manager.SetEncoding( entry, TextFileEncoding.Utf8 );
			Assert.True( entry.HasUnsavedChanges );
			manager.SetEncoding( entry, TextFileEncoding.Utf8Bom );
			Assert.False( entry.HasUnsavedChanges );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task FormatChangedDuringSaveRemainsDirtyBecauseOnlyWrittenFormatIsCheckpointed() {
		string directory = Directory.CreateTempSubdirectory( "dedit-format-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "text" );
			using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
			using DocumentManager manager = new( host );
			DocumentEntry? entry = null;
			DocumentFileService files = new( directory, beforeSaveStage: stage => {
				if ( stage == DocumentSaveStage.BeforeReplacement )
					manager.SetEncoding( entry!, TextFileEncoding.Utf16LittleEndian );
			} );
			entry = manager.Open( await files.OpenAsync( path, CancellationToken.None ) );
			manager.SetEncoding( entry, TextFileEncoding.Utf8Bom );
			await manager.SaveAsync( entry, files, CancellationToken.None );
			Assert.Equal( TextFileCodec.Encode( "text", TextFileEncoding.Utf8Bom ), await File.ReadAllBytesAsync( path ) );
			Assert.Equal( TextFileEncoding.Utf16LittleEndian, entry.Encoding );
			Assert.True( entry.HasUnsavedChanges );
		} finally { Directory.Delete( directory, true ); }
	}
	[Fact]
	public void FileEncodingMenuExposesAllSupportedFormats() {
		using WidgetHost host = new( new CursesScreen( 55, 18 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		DeditMenu.UpdateState( menu, entry.Shell.Editor, encoding: entry.Encoding );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'f' ) ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'e' ) ), TimeSpan.Zero );
		WidgetCheckedInvokedAction selected = Assert.IsType<WidgetCheckedInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'b' ) ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( DeditMenu.EncodingUtf8Bom, selected.CommandId );
		manager.SetEncoding( entry, TextFileEncoding.Utf8Bom );
		DeditMenu.UpdateState( menu, entry.Shell.Editor, encoding: entry.Encoding );
		Assert.True( entry.HasUnsavedChanges );
	}

}
