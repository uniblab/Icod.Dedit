/*
	Icod.Dedit
	Inspects proposed Save As targets without writing to disk.
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

using Icod.Widgets;

internal sealed class DeditSaveTargetProvider : IWidgetSaveTargetProvider {
	public ValueTask<WidgetSaveTargetObservation> InspectTargetAsync( string directory, string fileName,
		CancellationToken token ) {
		token.ThrowIfCancellationRequested();
		if ( string.IsNullOrWhiteSpace( fileName ) || fileName is "." or ".." ||
			fileName.IndexOfAny( [ Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ] ) >= 0 ||
			fileName.IndexOfAny( Path.GetInvalidFileNameChars() ) >= 0 || Path.IsPathRooted( fileName ) )
			return ValueTask.FromResult( new WidgetSaveTargetObservation( "", false, false, "Enter a file name without path separators." ) );
		try {
			string parent = Path.GetFullPath( directory );
			if ( !Directory.Exists( parent ) )
				return ValueTask.FromResult( new WidgetSaveTargetObservation( "", false, false, "The selected directory does not exist." ) );
			string path = Path.Combine( parent, fileName );
			bool exists = File.Exists( path ) || Directory.Exists( path );
			return ValueTask.FromResult( new WidgetSaveTargetObservation( path, exists, !exists,
				exists ? "The target already exists; choose another name." : null ) );
		} catch ( Exception exception ) when ( exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException ) {
			return ValueTask.FromResult( new WidgetSaveTargetObservation( "", false, false, exception.Message ) );
		}
	}
}
