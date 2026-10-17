/*
	Icod.Dedit
	Resolves file and parent-directory links for document identity.
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

internal static class PathIdentity {
	internal static string Resolve( string path ) {
		string fullPath = Path.GetFullPath( path );
		for ( int links = 0; links <= 64; links++ ) {
			string root = Path.GetPathRoot( fullPath )!;
			string[] segments = fullPath[ root.Length.. ].Split(
				[ Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ], StringSplitOptions.RemoveEmptyEntries );
			string current = root;
			bool redirected = false;
			for ( int index = 0; index < segments.Length; index++ ) {
				current = Path.Combine( current, segments[ index ] );
				FileSystemInfo item = index < segments.Length - 1 || Directory.Exists( current )
					? new DirectoryInfo( current ) : new FileInfo( current );
				if ( item.LinkTarget is null ) continue;
				FileSystemInfo? target = item.ResolveLinkTarget( false );
				if ( target is null ) continue;
				if ( links == 64 ) throw new IOException( "The path has too many symbolic links." );
				fullPath = target.FullName;
				for ( int remaining = index + 1; remaining < segments.Length; remaining++ )
					fullPath = Path.Combine( fullPath, segments[ remaining ] );
				fullPath = Path.GetFullPath( fullPath );
				redirected = true;
				break;
			}
			if ( !redirected ) return current;
		}
		throw new IOException( "The path has too many symbolic links." );
	}
}
