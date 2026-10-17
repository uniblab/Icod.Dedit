/*
	Icod.Dedit
	Decodes and encodes supported text files without changing their content.
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

namespace Icod.Dedit.Documents;

using System.Text;

internal enum TextFileEncoding { Utf8, Utf8Bom, Utf16LittleEndian, Utf16BigEndian }

internal sealed record DecodedText( string Text, TextFileEncoding Encoding ) {
	internal bool HasBom => this.Encoding != TextFileEncoding.Utf8;
}

internal static class TextFileCodec {
	private static readonly UTF8Encoding Utf8 = new( false, true );
	private static readonly UnicodeEncoding Utf16LittleEndian = new( false, true, true );
	private static readonly UnicodeEncoding Utf16BigEndian = new( true, true, true );
	private static ReadOnlySpan<byte> Utf8Marker => [ 0xEF, 0xBB, 0xBF ];
	private static ReadOnlySpan<byte> Utf16LittleEndianMarker => [ 0xFF, 0xFE ];
	private static ReadOnlySpan<byte> Utf16BigEndianMarker => [ 0xFE, 0xFF ];

	internal static DecodedText Decode( ReadOnlySpan<byte> bytes ) {
		TextFileEncoding format = TextFileEncoding.Utf8;
		Encoding decoder = Utf8;
		if ( bytes.StartsWith( Utf8Marker ) ) { format = TextFileEncoding.Utf8Bom; bytes = bytes[ 3.. ]; }
		else if ( bytes.StartsWith( Utf16LittleEndianMarker ) ) { format = TextFileEncoding.Utf16LittleEndian; decoder = Utf16LittleEndian; bytes = bytes[ 2.. ]; }
		else if ( bytes.StartsWith( Utf16BigEndianMarker ) ) { format = TextFileEncoding.Utf16BigEndian; decoder = Utf16BigEndian; bytes = bytes[ 2.. ]; }
		try {
			string text = decoder.GetString( bytes );
			if ( text.Contains( '\0' ) ) throw new InvalidDataException( "The file contains NUL characters and appears to be binary." );
			return new DecodedText( text, format );
		} catch ( DecoderFallbackException exception ) {
			throw new InvalidDataException( "The file is not valid UTF-8 or BOM-marked UTF-16 text.", exception );
		}
	}

	internal static byte[] Encode( string text, TextFileEncoding format ) {
		ArgumentNullException.ThrowIfNull( text );
		( Encoding encoder, byte[] marker ) = format switch {
			TextFileEncoding.Utf8 => ( (Encoding)Utf8, Array.Empty<byte>() ),
			TextFileEncoding.Utf8Bom => ( (Encoding)Utf8, Utf8Marker.ToArray() ),
			TextFileEncoding.Utf16LittleEndian => ( (Encoding)Utf16LittleEndian, Utf16LittleEndianMarker.ToArray() ),
			TextFileEncoding.Utf16BigEndian => ( (Encoding)Utf16BigEndian, Utf16BigEndianMarker.ToArray() ),
			_ => throw new ArgumentOutOfRangeException( nameof( format ) )
		};
		byte[] content = encoder.GetBytes( text );
		byte[] bytes = new byte[ checked( marker.Length + content.Length ) ];
		marker.CopyTo( bytes, 0 ); content.CopyTo( bytes, marker.Length );
		return bytes;
	}
}
