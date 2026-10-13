/*
	Icod.Dedit.Tests
	Checks Save As target inspection before writing.
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

using Icod.Dedit.Documents;
using Xunit;

public sealed class DeditSaveTargetProviderTests {
	[Fact]
	public async Task NewTargetIsValidAndExistingTargetRequiresAnotherName() {
		string directory = Directory.CreateTempSubdirectory( "dedit-target-" ).FullName;
		try {
			DeditSaveTargetProvider provider = new();
			var fresh = await provider.InspectTargetAsync( directory, "new.txt", CancellationToken.None );
			Assert.True( fresh.IsValid ); Assert.False( fresh.Exists );
			Assert.Equal( Path.Combine( directory, "new.txt" ), fresh.TargetPath );
			Assert.False( File.Exists( fresh.TargetPath ) );
			string existing = Path.Combine( directory, "existing.txt" );
			await File.WriteAllTextAsync( existing, "keep" );
			var observed = await provider.InspectTargetAsync( directory, "existing.txt", CancellationToken.None );
			Assert.False( observed.IsValid ); Assert.True( observed.Exists );
			Assert.Equal( "keep", await File.ReadAllTextAsync( existing ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Theory]
	[InlineData("../escape.txt")]
	[InlineData("sub/escape.txt")]
	[InlineData(".")]
	[InlineData("..")]
	public async Task NamesCannotEscapeChosenDirectory( string fileName ) {
		string directory = Directory.CreateTempSubdirectory( "dedit-target-" ).FullName;
		try {
			var result = await new DeditSaveTargetProvider().InspectTargetAsync( directory, fileName, CancellationToken.None );
			Assert.False( result.IsValid );
		} finally { Directory.Delete( directory, true ); }
	}
}
