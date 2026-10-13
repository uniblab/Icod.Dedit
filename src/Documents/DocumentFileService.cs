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
internal sealed record ExistingSaveTarget( string FullPath, string Identity, byte[] Fingerprint );
internal enum DocumentSaveStage { BeforeWrite, BeforeReplacement }

internal sealed class DocumentFileService {
	internal const int DefaultMaximumBytes = 32 * 1024 * 1024;
	private readonly string initialDirectory;
	private readonly int maximumBytes;
	private readonly Action<DocumentSaveStage>? beforeSaveStage;

	internal DocumentFileService( string initialDirectory, int maximumBytes = DefaultMaximumBytes,
		Action<DocumentSaveStage>? beforeSaveStage = null ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( initialDirectory );
		if ( !Path.IsPathFullyQualified( initialDirectory ) ) throw new ArgumentException( "The initial directory must be absolute.", nameof( initialDirectory ) );
		if ( maximumBytes < 1 ) throw new ArgumentOutOfRangeException( nameof( maximumBytes ) );
		this.initialDirectory = Path.GetFullPath( initialDirectory );
		this.maximumBytes = maximumBytes;
		this.beforeSaveStage = beforeSaveStage;
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
	internal async Task<ExistingSaveTarget> CaptureExistingAsync( string path, CancellationToken cancellationToken ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( path );
		cancellationToken.ThrowIfCancellationRequested();
		string fullPath = this.ResolvePath( path );
		string identity = ResolveTarget( fullPath );
		if ( ( File.GetAttributes( identity ) & FileAttributes.Directory ) != 0 )
			throw new IOException( "The save target is a directory." );
		await using FileStream stream = new( identity, new FileStreamOptions {
			Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.ReadWrite | FileShare.Delete,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		} );
		if ( stream.Length > this.maximumBytes ) throw new InvalidDataException( $"File exceeds the {this.maximumBytes} byte overwrite limit." );
		return new ExistingSaveTarget( fullPath, identity, SHA256.HashData( await this.ReadBoundedAsync( stream, cancellationToken ) ) );
	}

	internal string ResolvePath( string path ) => Path.GetFullPath( path, this.initialDirectory );

	internal async Task<byte[]> SaveNewAsync( string path, string text, TextFileEncoding encoding,
		CancellationToken cancellationToken ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( path );
		ArgumentNullException.ThrowIfNull( text );
		cancellationToken.ThrowIfCancellationRequested();
		string fullPath = this.ResolvePath( path );
		string target = ResolveTarget( fullPath );
		if ( File.Exists( target ) || Directory.Exists( target ) ) throw new IOException( "The save target already exists." );
		byte[] bytes = TextFileCodec.Encode( text, encoding );
		if ( bytes.Length > this.maximumBytes ) throw new InvalidDataException( $"File exceeds the {this.maximumBytes} byte save limit." );
		string temporary = Path.Combine( Path.GetDirectoryName( target )!,
			$".{Path.GetFileName( target )}.dedit-{Guid.NewGuid():N}.tmp" );
		try {
			await using ( FileStream stream = new( temporary, new FileStreamOptions {
				Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous
			} ) ) {
				this.beforeSaveStage?.Invoke( DocumentSaveStage.BeforeWrite );
				await stream.WriteAsync( bytes, cancellationToken );
				await stream.FlushAsync( cancellationToken );
			}
			cancellationToken.ThrowIfCancellationRequested();
			this.beforeSaveStage?.Invoke( DocumentSaveStage.BeforeReplacement );
			cancellationToken.ThrowIfCancellationRequested();
			if ( !PathComparer.Equals( ResolveTarget( fullPath ), target ) )
				throw new IOException( "The file target changed during the save." );
			cancellationToken.ThrowIfCancellationRequested();
			File.Move( temporary, target );
			return SHA256.HashData( bytes );
		} finally {
			if ( File.Exists( temporary ) ) File.Delete( temporary );
		}
	}

	internal async Task<byte[]> SaveAsync( string fullPath, string expectedIdentity, byte[] expectedFingerprint, string text,
		TextFileEncoding encoding, CancellationToken cancellationToken ) {
		ArgumentException.ThrowIfNullOrWhiteSpace( fullPath );
		ArgumentException.ThrowIfNullOrWhiteSpace( expectedIdentity );
		ArgumentNullException.ThrowIfNull( expectedFingerprint );
		ArgumentNullException.ThrowIfNull( text );
		cancellationToken.ThrowIfCancellationRequested();
		string path = Path.GetFullPath( fullPath, this.initialDirectory );
		string target = ResolveTarget( path );
		if ( !PathComparer.Equals( target, Path.GetFullPath( expectedIdentity ) ) )
			throw new IOException( "The file target changed since it was opened." );
		FileAttributes attributes = File.GetAttributes( target );
		if ( ( attributes & FileAttributes.ReadOnly ) != 0 ) throw new UnauthorizedAccessException( "The file is read-only." );
		UnixFileMode? mode = OperatingSystem.IsWindows() ? null : File.GetUnixFileMode( target );
		if ( mode is { } unixMode && ( unixMode & ( UnixFileMode.UserWrite | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite ) ) == 0 )
			throw new UnauthorizedAccessException( "The file is read-only." );
		await VerifyUnchangedAsync( target, expectedFingerprint, cancellationToken );
		byte[] bytes = TextFileCodec.Encode( text, encoding );
		if ( bytes.Length > this.maximumBytes ) throw new InvalidDataException( $"File exceeds the {this.maximumBytes} byte save limit." );
		string temporary = Path.Combine( Path.GetDirectoryName( target )!,
			$".{Path.GetFileName( target )}.dedit-{Guid.NewGuid():N}.tmp" );
		try {
			await using ( FileStream stream = new( temporary, new FileStreamOptions {
				Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous
			} ) ) {
				this.beforeSaveStage?.Invoke( DocumentSaveStage.BeforeWrite );
				await stream.WriteAsync( bytes, cancellationToken );
				await stream.FlushAsync( cancellationToken );
			}
			if ( !OperatingSystem.IsWindows() && mode is { } preservedMode ) File.SetUnixFileMode( temporary, preservedMode );
			cancellationToken.ThrowIfCancellationRequested();
			if ( !PathComparer.Equals( ResolveTarget( path ), target ) )
				throw new IOException( "The file target changed during the save." );
			await VerifyUnchangedAsync( target, expectedFingerprint, cancellationToken );
			cancellationToken.ThrowIfCancellationRequested();
			this.beforeSaveStage?.Invoke( DocumentSaveStage.BeforeReplacement );
			cancellationToken.ThrowIfCancellationRequested();
			if ( !PathComparer.Equals( ResolveTarget( path ), target ) )
				throw new IOException( "The file target changed during the save." );
			await VerifyUnchangedAsync( target, expectedFingerprint, cancellationToken );
			cancellationToken.ThrowIfCancellationRequested();
			File.Move( temporary, target, overwrite: true );
			return SHA256.HashData( bytes );
		} finally {
			if ( File.Exists( temporary ) ) File.Delete( temporary );
		}
	}

	private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	private static string ResolveTarget( string path ) =>
		PathIdentity.Resolve( path );

	private async Task VerifyUnchangedAsync( string path, byte[] fingerprint, CancellationToken cancellationToken ) {
		await using FileStream stream = new( path, new FileStreamOptions {
			Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.ReadWrite | FileShare.Delete,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		} );
		if ( stream.Length > this.maximumBytes || !SHA256.HashData( await ReadBoundedAsync( stream, cancellationToken ) ).AsSpan().SequenceEqual( fingerprint ) )
			throw new IOException( "The file changed since it was opened." );
	}

	private async Task<byte[]> ReadBoundedAsync( FileStream stream, CancellationToken cancellationToken ) {
		using MemoryStream buffer = new();
		byte[] chunk = new byte[ 81920 ];
		int count;
		while ( ( count = await stream.ReadAsync( chunk, cancellationToken ) ) != 0 ) {
			if ( count > this.maximumBytes - buffer.Length ) throw new IOException( "The file changed beyond the open limit." );
			buffer.Write( chunk, 0, count );
		}
		return buffer.ToArray();
	}
}
