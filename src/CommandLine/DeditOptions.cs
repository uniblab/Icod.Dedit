/*
	Icod.Dedit
	Parses the terminal editor's command line.
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

namespace Icod.Dedit.CommandLine;

internal enum DeditOptionMode { Edit, Help, Version }

internal sealed class DeditOptions {
	private DeditOptions( DeditOptionMode mode, string[] paths ) {
		this.Mode = mode;
		this.Paths = Array.AsReadOnly( paths );
		this.Captions = Array.AsReadOnly( paths.Length == 0 ? [ "Untitled" ] : paths.Select( path => {
			string name = Path.GetFileName( path );
			string caption = string.IsNullOrEmpty( name ) ? path : name;
			return new string( caption.Select( character => char.IsControl( character ) ? '�' : character ).ToArray() );
		} ).ToArray() );
	}

	public DeditOptionMode Mode { get; }
	public IReadOnlyList<string> Paths { get; }
	public IReadOnlyList<string> Captions { get; }

	public static DeditOptions Parse( IReadOnlyList<string> args ) {
		ArgumentNullException.ThrowIfNull( args );
		List<string> paths = []; bool positional = false;
		foreach ( string arg in args ) {
			if ( arg is null ) throw new ArgumentException( "Arguments cannot be null.", nameof( args ) );
			if ( !positional && arg == "--" ) { positional = true; continue; }
			if ( !positional && arg is "-h" or "--help" ) return new DeditOptions( DeditOptionMode.Help, [] );
			if ( !positional && arg is "-v" or "--version" ) return new DeditOptions( DeditOptionMode.Version, [] );
			if ( !positional && arg.StartsWith( "-", StringComparison.Ordinal ) ) throw new ArgumentException( $"Unknown option: {arg}" );
			if ( arg.Length == 0 ) throw new ArgumentException( "An empty path is not valid." );
			paths.Add( arg );
		}
		return new DeditOptions( DeditOptionMode.Edit, paths.ToArray() );
	}
}
