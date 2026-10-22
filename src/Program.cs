/*
	Icod.Dedit
	Entry point and diagnostics for the terminal text editor.
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

namespace Icod.Dedit;

using System.Diagnostics;
using Icod.Dedit.Application;
using Icod.Dedit.CommandLine;
using Icod.Dedit.Documents;

internal static class Program {
	public static Task<int> Main( string[] args ) => RunAsync( args, Console.Out, Console.Error );

	internal static async Task<int> RunAsync( string[] args, TextWriter output, TextWriter error,
		Func<ValueTask<IDeditTerminal>>? open = null ) {
		ArgumentNullException.ThrowIfNull( output ); ArgumentNullException.ThrowIfNull( error );
		DeditOptions options;
		try { options = DeditOptions.Parse( args ); }
		catch ( ArgumentException exception ) { await error.WriteLineAsync( $"dedit: {DocumentManager.SanitizeDisplay( exception.Message )}" ); return 2; }
		if ( options.Mode == DeditOptionMode.Help ) {
			await output.WriteLineAsync( "Usage: dedit [--help|-h] [--version|-v] [--] [FILE ...]\nCtrl+Q exits the current shell." );
			return 0;
		}
		if ( options.Mode == DeditOptionMode.Version ) {
			await output.WriteLineAsync( $"dedit {typeof( Program ).Assembly.GetName().Version?.ToString( 3 ) ?? "1.0.0"}" );
			return 0;
		}
		try {
			Stopwatch clock = Stopwatch.StartNew();
			await DeditApplication.RunAsync( options, open ?? ( () => DeditTerminal.OpenAsync( options ) ), () => clock.Elapsed );
			return 0;
		} catch ( Exception exception ) when ( exception is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or AggregateException ) {
			await error.WriteLineAsync( $"dedit: {DocumentManager.SanitizeDisplay( exception.Message )}" ); return 1;
		}
	}
}
