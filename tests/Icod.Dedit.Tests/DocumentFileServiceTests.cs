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

	[Fact]
	public async Task SavePreservesSelectedEncodingAndMixedTerminators() {
		string directory = Directory.CreateTempSubdirectory( "dedit-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			byte[] original = TextFileCodec.Encode( "old\r\n", TextFileEncoding.Utf16BigEndian );
			await File.WriteAllBytesAsync( path, original );
			DocumentFileService service = new( directory );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			string edited = "界\r\nnext\nend\r";
			byte[] fingerprint = await service.SaveAsync( path, path, loaded.Fingerprint, edited,
				TextFileEncoding.Utf16BigEndian, CancellationToken.None );
			byte[] expected = TextFileCodec.Encode( edited, TextFileEncoding.Utf16BigEndian );
			Assert.Equal( expected, await File.ReadAllBytesAsync( path ) );
			Assert.Equal( SHA256.HashData( expected ), fingerprint );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveRefusesExternalChangeAndKeepsBothVersions() {
		string directory = Directory.CreateTempSubdirectory( "dedit-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService service = new( directory );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			await File.WriteAllTextAsync( path, "external" );
			await Assert.ThrowsAsync<IOException>( () => service.SaveAsync( path, path, loaded.Fingerprint,
				"editor", TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveRefusesReadOnlyTargetWithoutLeavingTemporaryFiles() {
		string directory = Directory.CreateTempSubdirectory( "dedit-save-" ).FullName;
		string path = Path.Combine( directory, "sample.txt" );
		try {
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService service = new( directory );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			File.SetAttributes( path, File.GetAttributes( path ) | FileAttributes.ReadOnly );
			await Assert.ThrowsAnyAsync<Exception>( () => service.SaveAsync( path, path, loaded.Fingerprint,
				"editor", TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Equal( "original", await File.ReadAllTextAsync( path ) );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally {
			if ( File.Exists( path ) ) File.SetAttributes( path, FileAttributes.Normal );
			Directory.Delete( directory, true );
		}
	}

	[Fact]
	public async Task CanceledSaveDoesNotTouchDestination() {
		string directory = Directory.CreateTempSubdirectory( "dedit-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService service = new( directory );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			using CancellationTokenSource canceled = new(); canceled.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>( () => service.SaveAsync( path, path,
				loaded.Fingerprint, "editor", TextFileEncoding.Utf8, canceled.Token ) );
			Assert.Equal( "original", await File.ReadAllTextAsync( path ) );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SaveNewCreatesExactBytesAndRejectsExistingTarget() {
		string directory = Directory.CreateTempSubdirectory( "dedit-new-save-" ).FullName;
		try {
			DocumentFileService service = new( directory );
			string path = Path.Combine( directory, "sample.txt" );
			byte[] fingerprint = await service.SaveNewAsync( "sample.txt", "界\r\nnext", TextFileEncoding.Utf8Bom, CancellationToken.None );
			byte[] expected = TextFileCodec.Encode( "界\r\nnext", TextFileEncoding.Utf8Bom );
			Assert.Equal( expected, await File.ReadAllBytesAsync( path ) );
			Assert.Equal( SHA256.HashData( expected ), fingerprint );
			await Assert.ThrowsAsync<IOException>( () => service.SaveNewAsync( path, "overwrite", TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Equal( expected, await File.ReadAllBytesAsync( path ) );
			Assert.Single( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task SavePreservesTerminalControlCharactersAsFileData() {
		string directory = Directory.CreateTempSubdirectory( "dedit-control-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "control.txt" );
			string exact = "a\u001b]52;c;payload\u0007\u009bb\r\n";
			DocumentFileService service = new( directory );

			await service.SaveNewAsync( path, exact, TextFileEncoding.Utf8, CancellationToken.None );

			Assert.Equal( TextFileCodec.Encode( exact, TextFileEncoding.Utf8 ), await File.ReadAllBytesAsync( path ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task InjectedWriteFailureLeavesOriginalBytesAndRemovesTemporaryFile() {
		string directory = Directory.CreateTempSubdirectory( "dedit-failed-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService service = new( directory, beforeSaveStage: stage => {
				if ( stage == DocumentSaveStage.BeforeWrite ) throw new IOException( "Injected write failure." );
			} );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			await Assert.ThrowsAsync<IOException>( () => service.SaveAsync( path, path, loaded.Fingerprint,
				"edited", TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Equal( "original", await File.ReadAllTextAsync( path ) );
			Assert.Equal( [ path ], Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task InjectedReplacementFailureLeavesOriginalBytesAndRemovesTemporaryFile() {
		string directory = Directory.CreateTempSubdirectory( "dedit-failed-save-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService service = new( directory, beforeSaveStage: stage => {
				if ( stage == DocumentSaveStage.BeforeReplacement ) throw new IOException( "Injected replacement failure." );
			} );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			await Assert.ThrowsAsync<IOException>( () => service.SaveAsync( path, path, loaded.Fingerprint,
				"edited", TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Equal( "original", await File.ReadAllTextAsync( path ) );
			Assert.Equal( [ path ], Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

	[Fact]
	public async Task InjectedNewFileReplacementFailureLeavesTargetAbsentAndRemovesTemporaryFile() {
		string directory = Directory.CreateTempSubdirectory( "dedit-failed-new-save-" ).FullName;
		try {
			DocumentFileService service = new( directory, beforeSaveStage: stage => {
				if ( stage == DocumentSaveStage.BeforeReplacement ) throw new IOException( "Injected replacement failure." );
			} );
			await Assert.ThrowsAsync<IOException>( () => service.SaveNewAsync( "draft.txt", "draft",
				TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Empty( Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}
	[Fact]
	public async Task ExternalEditAfterFinalPreflightIsNotOverwritten() {
		string directory = Directory.CreateTempSubdirectory( "dedit-last-check-" ).FullName;
		try {
			string path = Path.Combine( directory, "sample.txt" );
			await File.WriteAllTextAsync( path, "original" );
			DocumentFileService service = new( directory, beforeSaveStage: stage => {
				if ( stage == DocumentSaveStage.BeforeReplacement ) File.WriteAllText( path, "external" );
			} );
			LoadedTextFile loaded = await service.OpenAsync( path, CancellationToken.None );
			await Assert.ThrowsAsync<IOException>( () => service.SaveAsync( path, path, loaded.Fingerprint,
				"edited", TextFileEncoding.Utf8, CancellationToken.None ) );
			Assert.Equal( "external", await File.ReadAllTextAsync( path ) );
			Assert.Equal( [ path ], Directory.GetFiles( directory ) );
		} finally { Directory.Delete( directory, true ); }
	}

}
