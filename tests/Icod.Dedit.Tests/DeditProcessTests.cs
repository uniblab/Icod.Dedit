/*
	Icod.Dedit.Tests
	Process-level command-line checks for the terminal editor.
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

using System.Diagnostics;
using Xunit;

public sealed class DeditProcessTests {
	[Theory]
	[InlineData("--help", 0, "Usage: dedit")]
	[InlineData("--version", 0, "dedit 1.0.0")]
	[InlineData("--unknown", 2, "Unknown option: --unknown")]
	public async Task NoninteractiveArgumentsExitWithoutTerminal( string argument, int expectedExit, string expectedText ) {
		ProcessStartInfo start = new( "dotnet" ) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
		start.ArgumentList.Add( typeof( Program ).Assembly.Location ); start.ArgumentList.Add( argument );
		using Process process = Process.Start( start ) ?? throw new InvalidOperationException( "Could not launch dedit." );
		using CancellationTokenSource timeout = new( TimeSpan.FromSeconds( 15 ) );
		await process.WaitForExitAsync( timeout.Token );
		Assert.Equal( expectedExit, process.ExitCode );
		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();
		Assert.Contains( expectedText, expectedExit == 2 ? error : output );
		Assert.Equal( "", expectedExit == 2 ? output : error );
	}
}
