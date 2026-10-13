/*
	Icod.Dedit.Tests
	Verifies bounded file loading leaves failures explicit.
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

using System.Security.Cryptography;
using Icod.Dedit.Documents;
using Xunit;

public sealed class DocumentFileServiceTests {
	[Fact]
	public async Task OpenResolvesAgainstInitialDirectoryAndCapturesExactBytes() {
		string directory = Directory.CreateTempSubdirectory( "dedit-read-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			byte[] bytes = TextFileCodec.Encode( "界\r\nnext\n", TextFileEncoding.Utf16BigEndian );
			await File.WriteAllBytesAsync( path, bytes );
			DocumentFileService service = new( directory );
			LoadedTextFile result = await service.OpenAsync( "sample.txt", CancellationToken.None );
			Assert.Equal( path, result.FullPath );
			Assert.Equal( "界\r\nnext\n", result.Content.Text );
			Assert.Equal( TextFileEncoding.Utf16BigEndian, result.Content.Encoding );
			Assert.Equal( SHA256.HashData( bytes ), result.Fingerprint );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task MissingInvalidOversizeAndCanceledFilesFailWithoutAReplacementValue() {
		string directory = Directory.CreateTempSubdirectory( "dedit-read-" ).FullName;
		try {
			DocumentFileService service = new( directory, maximumBytes: 4 );
			await Assert.ThrowsAsync<FileNotFoundException>( async () => await service.OpenAsync( "missing", CancellationToken.None ) );
			await File.WriteAllBytesAsync( Path.Combine( directory, "large" ), [ 1, 2, 3, 4, 5 ] );
			await Assert.ThrowsAsync<InvalidDataException>( async () => await service.OpenAsync( "large", CancellationToken.None ) );
			await File.WriteAllBytesAsync( Path.Combine( directory, "invalid" ), [ 0xFF ] );
			await Assert.ThrowsAsync<InvalidDataException>( async () => await service.OpenAsync( "invalid", CancellationToken.None ) );
			using CancellationTokenSource canceled = new(); canceled.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>( async () => await service.OpenAsync( "invalid", canceled.Token ) );
		} finally { Directory.Delete( directory, true ); }
	}
}
