/*
	Icod.Dedit
	Loads bounded, versioned editor defaults without affecting document saves.
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

namespace Icod.Dedit.Configuration;

using System.Text.Json;
using Icod.Dedit.Documents;

internal sealed record DeditPreferences( int TabWidth, int IndentWidth, bool Wrap, TextFileEncoding DefaultEncoding ) {
	internal static DeditPreferences Default { get; } = new( 4, 4, false, TextFileEncoding.Utf8 );
	private const int MaximumBytes = 16_384;

	internal static async Task<DeditPreferences> LoadAsync( string path, CancellationToken token ) {
		try {
			await using FileStream stream = new( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
				4096, FileOptions.Asynchronous | FileOptions.SequentialScan );
			if ( stream.Length > MaximumBytes ) return Default;
			using MemoryStream bytes = new();
			byte[] chunk = new byte[ 4096 ];
			int count;
			while ( ( count = await stream.ReadAsync( chunk, token ) ) != 0 ) {
				if ( count > MaximumBytes - bytes.Length ) return Default;
				bytes.Write( chunk, 0, count );
			}
			using JsonDocument json = JsonDocument.Parse( bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 8 } );
			JsonElement root = json.RootElement;
			if ( root.ValueKind != JsonValueKind.Object || !root.TryGetProperty( "version", out JsonElement version ) ||
				!version.TryGetInt32( out int number ) || number != 1 ) return Default;
			int tabWidth = Default.TabWidth, indentWidth = Default.IndentWidth;
			bool wrap = Default.Wrap;
			TextFileEncoding encoding = Default.DefaultEncoding;
			if ( root.TryGetProperty( "tabWidth", out JsonElement tab ) &&
				( !tab.TryGetInt32( out tabWidth ) || tabWidth is < 1 or > 16 ) ) return Default;
			if ( root.TryGetProperty( "indentWidth", out JsonElement indent ) &&
				( !indent.TryGetInt32( out indentWidth ) || indentWidth is < 1 or > 16 ) ) return Default;
			if ( root.TryGetProperty( "wrap", out JsonElement wrapping ) ) {
				if ( wrapping.ValueKind is not ( JsonValueKind.True or JsonValueKind.False ) ) return Default;
				wrap = wrapping.GetBoolean();
			}
			if ( root.TryGetProperty( "defaultEncoding", out JsonElement format ) ) {
				encoding = format.ValueKind == JsonValueKind.String ? format.GetString() switch {
					"utf-8" => TextFileEncoding.Utf8,
					"utf-8-bom" => TextFileEncoding.Utf8Bom,
					"utf-16-le" => TextFileEncoding.Utf16LittleEndian,
					"utf-16-be" => TextFileEncoding.Utf16BigEndian,
					_ => (TextFileEncoding)( -1 )
				} : (TextFileEncoding)( -1 );
				if ( !Enum.IsDefined( encoding ) ) return Default;
			}
			return new DeditPreferences( tabWidth, indentWidth, wrap, encoding );
		} catch ( Exception exception ) when ( exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException ) {
			return Default;
		}
	}

	internal static string? DefaultPath() {
		string directory = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
		return string.IsNullOrWhiteSpace( directory ) ? null : Path.Combine( directory, "Icod.Dedit", "preferences.json" );
	}
}
