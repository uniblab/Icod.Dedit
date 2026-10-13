/*
	Icod.Dedit.Tests
	Checks revision-tagged literal and regex search behavior.
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
using Xunit;

public sealed class SearchControllerTests {
	[Fact]
	public void LiteralCaseAndUnicodeWholeWordRespectMarksAndConnectors() {
		SearchQuery query = new( "cat", IsRegex: false, MatchCase: false, WholeWord: true );
		SearchResult result = SearchController.Search( "tab-1", 7, "cat scatter Cat cat\u0301 cat_", query, CancellationToken.None );
		Assert.Equal( "tab-1", result.DocumentKey );
		Assert.Equal( 7, result.Revision );
		Assert.Equal( [ new SearchHit( 0, 3 ), new SearchHit( 12, 3 ) ], result.Hits );
		Assert.Equal( new SearchHit( 12, 3 ), result.Next( 1, previous: false ) );
		Assert.Equal( new SearchHit( 0, 3 ), result.Next( 20, previous: false ) );
		Assert.Equal( new SearchHit( 12, 3 ), result.Next( 0, previous: true ) );
	}

	[Fact]
	public void RegexZeroWidthSearchSkipsInteriorOfGraphemeAndTerminates() {
		SearchResult result = SearchController.Search( "tab", 0, "a\u0301😀",
			new SearchQuery( "(?=.)", IsRegex: true, MatchCase: true, WholeWord: false ), CancellationToken.None );
		Assert.Equal( [ new SearchHit( 0, 0 ), new SearchHit( 2, 0 ) ], result.Hits );
	}

	[Fact]
	public void InvalidPatternAndCancellationFailWithoutResults() {
		Assert.ThrowsAny<ArgumentException>( () => SearchController.Search( "tab", 0, "text",
			new SearchQuery( "[", IsRegex: true, MatchCase: true, WholeWord: false ), CancellationToken.None ) );
		using CancellationTokenSource source = new(); source.Cancel();
		Assert.ThrowsAny<OperationCanceledException>( () => SearchController.Search( "tab", 0, "text",
			new SearchQuery( "text", IsRegex: false, MatchCase: true, WholeWord: false ), source.Token ) );
	}
}
