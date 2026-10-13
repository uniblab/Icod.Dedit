/*
	Icod.Dedit.Tests
	Checks one-based grapheme positions for Go to Line.
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

using Icod.Dedit.Interaction;
using Icod.Widgets;
using Xunit;

public sealed class GoToCoordinateTests {
	[Theory]
	[InlineData( "1:1", 0 )]
	[InlineData( "1:2", 1 )]
	[InlineData( "1:3", 3 )]
	[InlineData( "1:4", 5 )]
	[InlineData( "1:5", 6 )]
	[InlineData( "2:1", 8 )]
	[InlineData( "2:2", 9 )]
	public void ResolvesGraphemeColumnsAcrossTabsCombiningMarksEmojiAndCrLf( string position, long offset ) {
		WidgetTextDocument document = new( [ "a\u0301\t😀x\r\nZ".AsMemory() ] );
		Assert.True( GoToCoordinate.TryResolve( document, position, out long actual ) );
		Assert.Equal( offset, actual );
	}

	[Theory]
	[InlineData( "" )]
	[InlineData( "1" )]
	[InlineData( "1:" )]
	[InlineData( ":1" )]
	[InlineData( "0:1" )]
	[InlineData( "1:0" )]
	[InlineData( "-1:2" )]
	[InlineData( "3:1" )]
	[InlineData( "2:3" )]
	[InlineData( "1:6" )]
	[InlineData( "999999999999999999999:1" )]
	[InlineData( "1:999999999999999999999" )]
	[InlineData( "1:2:3" )]
	public void RejectsMissingOrOutOfRangePositions( string position ) {
		WidgetTextDocument document = new( [ "a\u0301\t😀x\r\nZ".AsMemory() ] );
		Assert.False( GoToCoordinate.TryResolve( document, position, out _ ) );
	}
}
