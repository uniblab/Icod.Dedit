/*
	Icod.Dedit.Tests
	Creates symbolic links for tests that require host permission.
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

using Xunit;

internal static class SymlinkTestSupport {
	private const int PrivilegeNotHeldHResult = unchecked( (int)0x80070522 );

	internal static void CreateDirectory( string link, string target ) {
		try { Directory.CreateSymbolicLink( link, target ); }
		catch ( IOException ex ) when ( OperatingSystem.IsWindows() && ex.HResult == PrivilegeNotHeldHResult ) {
			Skip.If( true, "Windows has not granted this process permission to create symbolic links." );
		}
	}

	internal static void CreateFile( string link, string target ) {
		try { File.CreateSymbolicLink( link, target ); }
		catch ( IOException ex ) when ( OperatingSystem.IsWindows() && ex.HResult == PrivilegeNotHeldHResult ) {
			Skip.If( true, "Windows has not granted this process permission to create symbolic links." );
		}
	}
}
