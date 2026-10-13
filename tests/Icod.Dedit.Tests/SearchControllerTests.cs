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
using Icod.Dedit.Documents;
using Icod.DCurses;
using Icod.Widgets;
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

	[Fact]
	public void ReplaceAllIsOneUndoTransactionAndRejectsStaleRevision() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "one One ONE" ), TimeSpan.Zero );
		SearchQuery query = new( "one", IsRegex: false, MatchCase: false, WholeWord: true );
		SearchResult result = SearchController.Search( entry.Key, entry.Shell.Document.Revision,
			entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ), query, CancellationToken.None );
		Assert.Equal( WidgetTextEditStatus.Applied,
			SearchController.ReplaceAll( entry, result, "two", TimeSpan.FromMilliseconds( 1 ) ) );
		Assert.Equal( "two two two", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
		entry.Shell.Document.Undo();
		Assert.Equal( "one One ONE", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
		Assert.Equal( WidgetTextEditStatus.Stale,
			SearchController.ReplaceAll( entry, result, "bad", TimeSpan.FromMilliseconds( 2 ) ) );
		Assert.Equal( "one One ONE", entry.Shell.Document.Read( 0, checked( (int)entry.Shell.Document.Length ) ) );
	}

	[Fact]
	public void RegexReplaceAllExpandsCapturesButLiteralReplacementKeepsDollarText() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "a1 b2" ), TimeSpan.Zero );
		SearchResult regex = SearchController.Search( entry.Key, entry.Shell.Document.Revision, "a1 b2",
			new SearchQuery( @"([a-z])(\d)", true, true, false ), CancellationToken.None );
		Assert.Equal( WidgetTextEditStatus.Applied,
			SearchController.ReplaceAll( entry, regex, "$2$1", TimeSpan.FromMilliseconds( 1 ) ) );
		Assert.Equal( "1a 2b", entry.Shell.Document.Read( 0, 5 ) );
		SearchResult literal = SearchController.Search( entry.Key, entry.Shell.Document.Revision, "1a 2b",
			new SearchQuery( "1a", false, true, false ), CancellationToken.None );
		Assert.Equal( WidgetTextEditStatus.Applied,
			SearchController.ReplaceAll( entry, literal, "$1", TimeSpan.FromMilliseconds( 2 ) ) );
		Assert.Equal( "$1 2b", entry.Shell.Document.Read( 0, 5 ) );
	}
}
