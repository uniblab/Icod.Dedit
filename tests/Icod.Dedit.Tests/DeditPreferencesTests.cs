/*
	Icod.Dedit.Tests
	Checks bounded versioned editor preferences and safe defaults.
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

using Icod.Dedit.Configuration;
using Icod.Dedit.Documents;
using Xunit;

public sealed class DeditPreferencesTests {
	[Fact]
	public async Task ValidVersionedPreferencesLoadButMalformedAndOversizedFilesFallBack() {
		string directory = Directory.CreateTempSubdirectory( "dedit-preferences-" ).FullName;
		try {
			string path = Path.Combine( directory, "preferences.json" );
			Assert.Equal( DeditPreferences.Default, await DeditPreferences.LoadAsync( path, CancellationToken.None ) );
			await File.WriteAllTextAsync( path,
				"{\"version\":1,\"tabWidth\":8,\"indentWidth\":2,\"wrap\":true,\"defaultEncoding\":\"utf-8-bom\"}" );
			Assert.Equal( new DeditPreferences( 8, 2, true, TextFileEncoding.Utf8Bom ),
				await DeditPreferences.LoadAsync( path, CancellationToken.None ) );
			await File.WriteAllTextAsync( path, "{\"version\":3,\"tabWidth\":-1}" );
			Assert.Equal( DeditPreferences.Default, await DeditPreferences.LoadAsync( path, CancellationToken.None ) );
			await File.WriteAllTextAsync( path, "{\\"version\\":1,\\"tabWidth\\":\\"wide\\"}" );
			Assert.Equal( DeditPreferences.Default, await DeditPreferences.LoadAsync( path, CancellationToken.None ) );
			await File.WriteAllTextAsync( path, "{ broken" );
			Assert.Equal( DeditPreferences.Default, await DeditPreferences.LoadAsync( path, CancellationToken.None ) );
			await File.WriteAllTextAsync( path, new string( 'x', 20_000 ) );
			Assert.Equal( DeditPreferences.Default, await DeditPreferences.LoadAsync( path, CancellationToken.None ) );
		} finally { Directory.Delete( directory, true ); }
	}
}
