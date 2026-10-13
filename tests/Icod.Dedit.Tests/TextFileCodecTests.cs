/*
	Icod.Dedit.Tests
	Checks strict file text decoding and byte-exact encoding round trips.
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
using Icod.Dedit.Documents;
using Xunit;

public sealed class TextFileCodecTests {
	[Theory]
	[InlineData( 0, "one\r\ntwo\nthree\rfour", false )]
	[InlineData( 1, "one\r\ntwo\nthree\rfour\n", true )]
	[InlineData( 2, "界\r\nsecond\r", true )]
	[InlineData( 3, "α\nβ", true )]
	[InlineData( 0, "", false )]
	[InlineData( 1, "", true )]
	[InlineData( 2, "", true )]
	[InlineData( 3, "", true )]
	public void SupportedFormatRoundTripsWithoutNormalizing( int encodingCode, string text, bool hasBom ) {
		TextFileEncoding encoding = (TextFileEncoding)encodingCode;
		byte[] bytes = TextFileCodec.Encode( text, encoding );
		DecodedText decoded = TextFileCodec.Decode( bytes );
		Assert.Equal( encoding, decoded.Encoding );
		Assert.Equal( text, decoded.Text );
		Assert.Equal( bytes, TextFileCodec.Encode( decoded.Text, decoded.Encoding ) );
		Assert.Equal( hasBom, decoded.HasBom );
	}

	[Fact]
	public void InvalidAndBinaryLookingInputsAreRejectedWithoutReplacement() {
		Assert.Throws<InvalidDataException>( () => TextFileCodec.Decode( [ 0xC3, 0x28 ] ) );
		Assert.Throws<InvalidDataException>( () => TextFileCodec.Decode( [ 0xEF, 0xBB, 0xBF, 0xFF ] ) );
		Assert.Throws<InvalidDataException>( () => TextFileCodec.Decode( [ 0xFF, 0xFE, 0x00 ] ) );
		Assert.Throws<InvalidDataException>( () => TextFileCodec.Decode( [ 0xFE, 0xFF, 0xD8, 0x00 ] ) );
		Assert.Throws<InvalidDataException>( () => TextFileCodec.Decode( [ 0x61, 0x00, 0x62, 0x00 ] ) );
		Assert.Throws<InvalidDataException>( () => TextFileCodec.Decode( [ 0x00 ] ) );
	}

	[Fact]
	public void UnsupportedEncodingAndInvalidUtf16CannotBeEncodedSilently() {
		Assert.Throws<ArgumentOutOfRangeException>( () => TextFileCodec.Encode( "text", (TextFileEncoding)999 ) );
		Assert.Throws<EncoderFallbackException>( () => TextFileCodec.Encode( "\uD800", TextFileEncoding.Utf8 ) );
		Assert.Throws<EncoderFallbackException>( () => TextFileCodec.Encode( "\uD800", TextFileEncoding.Utf16LittleEndian ) );
	}
}
