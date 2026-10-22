/*
	Icod.Dedit
	Converts one-based logical line and grapheme column to a document offset.
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

using System.Globalization;
using Icod.Widgets;

internal static class GoToCoordinate {
	internal static bool TryResolve( WidgetTextDocument document, string input, out long offset ) {
		ArgumentNullException.ThrowIfNull( document );
		offset = 0;
		if ( input is null ) return false;
		int separator = input.IndexOf( ':' );
		if ( separator <= 0 || separator == input.Length - 1 || input.IndexOf( ':', separator + 1 ) >= 0 ||
			!long.TryParse( input.AsSpan( 0, separator ), NumberStyles.None, CultureInfo.InvariantCulture, out long lineNumber ) ||
			!long.TryParse( input.AsSpan( separator + 1 ), NumberStyles.None, CultureInfo.InvariantCulture, out long columnNumber ) ||
			lineNumber < 1 || columnNumber < 1 || lineNumber > document.LineCount ) return false;
		WidgetTextLine line = document.GetLine( lineNumber - 1 );
		if ( line.ContentLength > int.MaxValue ) return false;
		string content = document.Read( line.Start, checked( (int)line.ContentLength ) );
		int[] starts = StringInfo.ParseCombiningCharacters( content );
		if ( columnNumber > (long)starts.Length + 1 ) return false;
		offset = line.Start + ( columnNumber == (long)starts.Length + 1 ? content.Length : starts[ checked( (int)columnNumber - 1 ) ] );
		return document.IsTextElementBoundary( offset );
	}
}
