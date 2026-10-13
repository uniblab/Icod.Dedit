/*
	Icod.Dedit
	Loads bounded document snapshots with explicit path and decoding errors.
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

using System.Security.Cryptography;

internal sealed record LoadedTextFile( string FullPath, DecodedText Content, byte[] Fingerprint );

internal sealed class DocumentFileService {
	internal const int DefaultMaximumBytes = 32 * 1024 * 1024;
	private readonly string initialDirectory;
	private readonly int maximumBytes;

	internal DocumentFileService( string initialDirectory, int maximumBytes = DefaultMaximumBytes ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( initialDirectory );
		if ( !Path.IsPathFullyQualified( initialDirectory ) ) throw new ArgumentException( "The initial directory must be absolute.", nameof( initialDirectory ) );
		if ( maximumBytes < 1 ) throw new ArgumentOutOfRangeException( nameof( maximumBytes ) );
		this.initialDirectory = Path.GetFullPath( initialDirectory );
		this.maximumBytes = maximumBytes;
	}

	internal async Task<LoadedTextFile> OpenAsync( string path, CancellationToken cancellationToken ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( path );
		cancellationToken.ThrowIfCancellationRequested();
		string fullPath = Path.GetFullPath( path, this.initialDirectory );
		await using FileStream stream = new( fullPath, new FileStreamOptions {
			Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.ReadWrite | FileShare.Delete,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		} );
		if ( stream.Length > this.maximumBytes ) throw new InvalidDataException( $"File exceeds the {this.maximumBytes} byte open limit." );
		using MemoryStream buffer = new( checked( (int)stream.Length ) );
		byte[] chunk = new byte[ 81920 ];
		int count;
		while ( ( count = await stream.ReadAsync( chunk, cancellationToken ) ) != 0 ) {
			if ( count > this.maximumBytes - buffer.Length ) throw new InvalidDataException( $"File exceeds the {this.maximumBytes} byte open limit." );
			buffer.Write( chunk, 0, count );
		}
		byte[] bytes = buffer.ToArray();
		return new LoadedTextFile( fullPath, TextFileCodec.Decode( bytes ), SHA256.HashData( bytes ) );
	}
	internal Task<byte[]> SaveAsync( string fullPath, string expectedIdentity, byte[] expectedFingerprint, string text,
		TextFileEncoding encoding, CancellationToken cancellationToken ) => throw new NotImplementedException();
}
