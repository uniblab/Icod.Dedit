/*
	Icod.Dedit
	Searches immutable document text with bounded regex work.
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

namespace Icod.Dedit.Interaction;

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Icod.Dedit.Documents;
using Icod.Widgets;

internal sealed record SearchQuery( string Pattern, bool IsRegex, bool MatchCase, bool WholeWord );
internal sealed record SearchHit( int Start, int Length );

internal sealed class SearchResult( string documentKey, long revision, SearchQuery query, IReadOnlyList<SearchHit> hits ) {
	internal SearchQuery Query { get; } = query;
	internal string DocumentKey { get; } = documentKey;
	internal long Revision { get; } = revision;
	internal IReadOnlyList<SearchHit> Hits { get; } = hits.ToArray();

	internal SearchHit? Next( int offset, bool previous ) {
		if ( this.Hits.Count == 0 ) return null;
		return previous
			? this.Hits.LastOrDefault( hit => hit.Start < offset ) ?? this.Hits[ ^1 ]
			: this.Hits.FirstOrDefault( hit => hit.Start > offset || hit.Start == offset && hit.Length > 0 ) ?? this.Hits[ 0 ];
	}
}

internal static class SearchController {
	private const int MaximumMatches = 100_000;
	private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds( 100 );
	private static readonly TimeSpan TotalBudget = TimeSpan.FromSeconds( 2 );

	internal static SearchResult Search( string documentKey, long revision, string text, SearchQuery query, CancellationToken token ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( documentKey );
		ArgumentNullException.ThrowIfNull( text );
		ArgumentNullException.ThrowIfNull( query );
		if ( string.IsNullOrEmpty( query.Pattern ) ) throw new ArgumentException( "Search text cannot be empty.", nameof( query ) );
		token.ThrowIfCancellationRequested();
		RegexOptions options = RegexOptions.CultureInvariant;
		if ( !query.MatchCase ) options |= RegexOptions.IgnoreCase;
		Regex expression = new( query.IsRegex ? query.Pattern : Regex.Escape( query.Pattern ), options, RegexTimeout );
		List<SearchHit> hits = [];
		Stopwatch timer = Stopwatch.StartNew();
		int offset = 0;
		int boundary = 0;
		while ( offset <= text.Length ) {
			token.ThrowIfCancellationRequested();
			if ( timer.Elapsed > TotalBudget ) throw new TimeoutException( "Search exceeded its time budget." );
			Match match = expression.Match( text, offset );
			if ( !match.Success ) break;
			bool valid = true;
			if ( match.Length == 0 ) {
				while ( boundary < match.Index ) {
					int units = StringInfo.GetNextTextElementLength( text, boundary );
					boundary += Math.Max( units, 1 );
				}
				valid = boundary == match.Index;
			}
			if ( valid && ( !query.WholeWord || IsWholeWord( text, match.Index, match.Index + match.Length ) ) ) {
				if ( hits.Count == MaximumMatches ) throw new InvalidDataException( "Search has too many matches." );
				hits.Add( new SearchHit( match.Index, match.Length ) );
			}
			if ( match.Length > 0 ) offset = match.Index + match.Length;
			else if ( match.Index == text.Length ) break;
			else {
				if ( boundary == match.Index )
					boundary += Math.Max( StringInfo.GetNextTextElementLength( text, boundary ), 1 );
				offset = Math.Max( match.Index + 1, boundary );
			}
		}
		return new SearchResult( documentKey, revision, query, hits );
	}

	internal static WidgetTextEditStatus ReplaceAll( DocumentEntry entry, SearchResult result, string replacement, TimeSpan timestamp,
		CancellationToken token = default ) {
		ArgumentNullException.ThrowIfNull( entry );
		ArgumentNullException.ThrowIfNull( result );
		ArgumentNullException.ThrowIfNull( replacement );
		token.ThrowIfCancellationRequested();
		Stopwatch timer = Stopwatch.StartNew();
		WidgetTextDocument document = entry.Shell.Document;
		if ( result.DocumentKey != entry.Key || result.Revision != document.Revision ) return WidgetTextEditStatus.Stale;
		if ( result.Hits.Count == 0 ) return WidgetTextEditStatus.NoChange;
		using WidgetTextSnapshot snapshot = document.CreateSnapshot();
		string text = snapshot.Read( 0, checked( (int)snapshot.Length ) );
		Regex? expression = null;
		if ( result.Query.IsRegex ) {
			RegexOptions options = RegexOptions.CultureInvariant;
			if ( !result.Query.MatchCase ) options |= RegexOptions.IgnoreCase;
			expression = new Regex( result.Query.Pattern, options, RegexTimeout );
		}
		List<WidgetTextEdit> edits = [];
		foreach ( SearchHit hit in result.Hits ) {
			token.ThrowIfCancellationRequested();
			if ( timer.Elapsed > TotalBudget ) throw new TimeoutException( "Replacement exceeded its time budget." );
			string inserted = replacement;
			if ( expression is not null ) {
				Match match = expression.Match( text, hit.Start );
				if ( !match.Success || match.Index != hit.Start || match.Length != hit.Length )
					return WidgetTextEditStatus.Stale;
				inserted = match.Result( replacement );
			}
			edits.Add( new WidgetTextEdit( hit.Start, hit.Length, inserted ) );
		}
		token.ThrowIfCancellationRequested();
		if ( timer.Elapsed > TotalBudget ) throw new TimeoutException( "Replacement exceeded its time budget." );
		WidgetTextViewState view = entry.Shell.Editor.View;
		WidgetTextEditContext context = new( view.Id, view.Anchor, view.Caret, 0, 0,
			WidgetTextEditKind.Programmatic, timestamp );
		return document.Apply( edits, result.Revision, context ).Status;
	}

	private static bool IsWholeWord( string text, int start, int end ) =>
		( start == 0 || !IsWordBefore( text, start ) ) && ( end == text.Length || !IsWordAfter( text, end ) );

	private static bool IsWordBefore( string text, int offset ) =>
		Rune.DecodeLastFromUtf16( text.AsSpan( 0, offset ), out Rune value, out _ ) == OperationStatus.Done && IsWord( value );

	private static bool IsWordAfter( string text, int offset ) =>
		Rune.DecodeFromUtf16( text.AsSpan( offset ), out Rune value, out _ ) == OperationStatus.Done && IsWord( value );

	private static bool IsWord( Rune value ) => Rune.GetUnicodeCategory( value ) is
		UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or
		UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber or
		UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber or UnicodeCategory.NonSpacingMark or
		UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark or UnicodeCategory.ConnectorPunctuation;
}
