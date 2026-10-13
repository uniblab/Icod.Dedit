/*
	Icod.Dedit
	Formats the active document status without scanning the whole buffer.
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

namespace Icod.Dedit.Presentation;

using System.Globalization;
using Icod.Dedit.Documents;
using Icod.Widgets;

internal static class EditorStatus {
	private const int MaximumColumnScan = 4_096;

	internal static string Format( DocumentEntry entry ) {
		ArgumentNullException.ThrowIfNull( entry );
		WidgetTextDocument document = entry.Shell.Document;
		long caret = entry.Shell.Editor.View.Caret;
		long lineIndex = document.GetLineIndex( caret );
		WidgetTextLine line = document.GetLine( lineIndex );
		long localCaret = Math.Clamp( caret - line.Start, 0, line.ContentLength );
		string column = localCaret > MaximumColumnScan ? $">{MaximumColumnScan}" :
			checked( StringInfo.ParseCombiningCharacters( document.Read( line.Start, checked( (int)localCaret ) ) ).Length + 1 )
				.ToString( CultureInfo.InvariantCulture );
		return $"{DocumentManager.SanitizeDisplay( entry.Caption )} | " +
			$"Ln {lineIndex + 1}, Col {column} | {EncodingLabel( entry.Encoding )} | " +
			$"{LineEndingLabel( document, line )} | Indent {entry.Shell.IndentationWidth} | " +
			$"{( entry.Shell.Editor.IsOverwriteMode ? "OVR" : "INS" )}";
	}

	private static string LineEndingLabel( WidgetTextDocument document, WidgetTextLine line ) {
		if ( line.LineEndingLength == 0 ) return "EOF";
		if ( line.LineEndingLength == 2 ) return "CRLF";
		return document.Read( line.Start + line.ContentLength, 1 ) == "\r" ? "CR" : "LF";
	}

	internal static string EncodingLabel( TextFileEncoding encoding ) => encoding switch {
		TextFileEncoding.Utf8 => "UTF-8",
		TextFileEncoding.Utf8Bom => "UTF-8 BOM",
		TextFileEncoding.Utf16LittleEndian => "UTF-16 LE",
		TextFileEncoding.Utf16BigEndian => "UTF-16 BE",
		_ => throw new ArgumentOutOfRangeException( nameof( encoding ) )
	};
}
