/*
	Icod.Dedit.Tests
	Checks the active-document status summary.
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
using Icod.Dedit.Configuration;
using Icod.Dedit.Documents;
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class EditorStatusTests {
	[Fact]
	public void StatusReportsGraphemePositionCurrentEndingIndentEncodingAndMode() {
		using WidgetHost host = new( new CursesScreen( 80, 12 ), WidgetTheme.Default );
		using DocumentManager manager = new( host,
			new DeditPreferences( TabWidth: 8, IndentWidth: 2, Wrap: false, DefaultEncoding: TextFileEncoding.Utf8Bom ) );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "a\r\nb\u0301\nlast" ), TimeSpan.Zero );
		entry.Shell.Editor.View.SetSelection( 5, 5 );
		entry.Shell.Process( WidgetInput.Key( WidgetKey.Insert ), TimeSpan.FromMilliseconds( 1 ) );

		Assert.Equal( "Untitled | Ln 2, Col 2 | UTF-8 BOM | LF | Indent 2 | OVR",
			EditorStatus.Format( entry ) );
	}

	[Fact]
	public void WrappedVerticalMovementKeepsLogicalLineAndCharacterStatus() {
		using WidgetHost host = new( new CursesScreen( 8, 6 ), WidgetTheme.Default );
		using DocumentManager manager = new( host,
			new DeditPreferences( TabWidth: 4, IndentWidth: 4, Wrap: true, DefaultEncoding: TextFileEncoding.Utf8 ) );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "abcdefghij" ), TimeSpan.Zero );
		entry.Shell.Editor.View.SetSelection( 2, 2 );

		entry.Shell.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );

		Assert.Equal( 8, entry.Shell.Editor.View.Caret );
		Assert.Contains( "Ln 1, Col 9", EditorStatus.Format( entry ), StringComparison.Ordinal );
	}

	[Theory]
	[InlineData( "one\r\ntwo", 1, "CRLF" )]
	[InlineData( "one\rtwo", 1, "CR" )]
	[InlineData( "one\ntwo", 5, "EOF" )]
	public void StatusDescribesTheCurrentLogicalLineEnding( string text, long caret, string expected ) {
		using WidgetHost host = new( new CursesScreen( 60, 10 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( text ), TimeSpan.Zero );
		entry.Shell.Editor.View.SetSelection( caret, caret );

		Assert.Contains( $" | {expected} | ", EditorStatus.Format( entry ), StringComparison.Ordinal );
	}

	[Fact]
	public void StatusBoundsColumnWorkForAVeryLongLogicalLine() {
		using WidgetHost host = new( new CursesScreen( 60, 10 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		string text = new( 'x', 1_000_000 );
		entry.Shell.Process( WidgetInput.Paste( text ), TimeSpan.Zero );

		long before = GC.GetAllocatedBytesForCurrentThread();
		string status = EditorStatus.Format( entry );

		Assert.Contains( "Ln 1, Col >4096", status, StringComparison.Ordinal );
		Assert.InRange( GC.GetAllocatedBytesForCurrentThread() - before, 0, 250_000 );
	}

	[Fact]
	public void StatusExplicitlyReportsAnActiveSelection() {
		using WidgetHost host = new( new CursesScreen( 60, 10 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "selected text" ), TimeSpan.Zero );
		entry.Shell.Editor.View.SetSelection( 0, 8 );

		Assert.Contains( " | SEL | INS", EditorStatus.Format( entry ), StringComparison.Ordinal );
	}
}
