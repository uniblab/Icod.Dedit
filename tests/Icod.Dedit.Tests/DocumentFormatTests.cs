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
using Icod.Dedit.Interaction;
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

	[Fact]
	public void NormalizeMixedLineEndingsPreservesFinalTerminatorAndIsOneUndoStep() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "one\r\ntwo\nthree\rfour\r" ), TimeSpan.Zero );
		entry.Shell.Editor.View.SetSelection( 5, 13 );
		Assert.Equal( WidgetTextEditStatus.Applied,
			DocumentFormatController.NormalizeLineEndings( entry, "\n", TimeSpan.FromMilliseconds( 1 ) ) );
		Assert.Equal( "one\ntwo\nthree\nfour\n",
			entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
		Assert.Equal( 4, entry.Shell.Editor.View.SelectionStart );
		Assert.Equal( 12, entry.Shell.Editor.View.SelectionEnd );
		entry.Shell.Document.Undo();
		Assert.Equal( "one\r\ntwo\nthree\rfour\r",
			entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
	}

	[Theory]
	[InlineData( "\r\n", "first\r\nsecond\r\n" )]
	[InlineData( "\r", "first\rsecond\r" )]
	public void ConversionAlsoSetsFutureEnterPolicy( string ending, string expected ) {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "first\nsecond" ), TimeSpan.Zero );
		Assert.Equal( WidgetTextEditStatus.Applied,
			DocumentFormatController.NormalizeLineEndings( entry, ending, TimeSpan.FromSeconds( 1 ) ) );
		entry.Shell.Editor.View.SetSelection( entry.Shell.Document.Length, entry.Shell.Document.Length );
		entry.Shell.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromSeconds( 2 ) );
		Assert.Equal( expected, entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
	}

	[Fact]
	public void ChoosingLineEndingForUntitledWithoutTerminatorsChangesFutureEnterWithoutDirtying() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		Assert.Equal( WidgetTextEditStatus.NoChange,
			DocumentFormatController.NormalizeLineEndings( entry, "\r\n", TimeSpan.Zero ) );
		Assert.False( entry.HasUnsavedChanges );
		entry.Shell.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromSeconds( 1 ) );
		Assert.Equal( "\r\n", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
	}

	[Fact]
	public void FileLineEndingMenuOffersExplicitCrLfConversion() {
		using WidgetHost host = new( new CursesScreen( 55, 18 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		DeditMenu.UpdateState( menu, entry.Shell.Editor );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'f' ) ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 't' ) ), TimeSpan.Zero );
		WidgetInvokedAction selected = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'w' ) ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( DeditMenu.LineEndingCrLf, selected.CommandId );
	}

}
