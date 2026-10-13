/*
	Icod.Dedit
	Owns stable tab identities and independent editor documents.
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

using Icod.Dedit.Presentation;
using Icod.Widgets;

internal sealed class DocumentEntry( string key, string caption, string? fullPath, string? identity, TextFileEncoding encoding,
	byte[] fingerprint, EditorShell shell ) {
	internal string Key { get; } = key;
	internal string Caption { get; set; } = caption;
	internal string? FullPath { get; } = fullPath;
	internal string? Identity { get; } = identity;
	internal TextFileEncoding Encoding { get; } = encoding;
	internal byte[] Fingerprint { get; } = fingerprint;
	internal EditorShell Shell { get; } = shell;
}

internal sealed class DocumentManager : IDisposable {
	private readonly WidgetHost host;
	private readonly List<DocumentEntry> documents = [];
	private readonly StringComparer pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
	private int nextUntitled;
	private bool disposed;

	internal DocumentManager( WidgetHost host ) => this.host = host ?? throw new ArgumentNullException( nameof( host ) );
	internal DocumentEntry? Active { get; private set; }
	internal IReadOnlyList<WidgetTab> Tabs => this.documents.Select( document => new WidgetTab( document.Key, SanitizeDisplay( document.Caption ),
		IsModified: document.Shell.Document.IsModified, IsClosable: false ) ).ToArray();
	internal static string SanitizeDisplay( string text ) => new( text.Select( character => char.IsControl( character ) ? '�' : character ).ToArray() );

	internal DocumentEntry OpenUntitled() => this.Add( $"Untitled{( ++this.nextUntitled == 1 ? "" : $" {this.nextUntitled}" )}", null, null,
		TextFileEncoding.Utf8, [], "" );

	internal DocumentEntry Open( LoadedTextFile loaded ) {
		ArgumentNullException.ThrowIfNull( loaded );
		this.ThrowIfDisposed();
		string fullPath = Path.GetFullPath( loaded.FullPath );
		string identity = ResolveIdentity( fullPath );
		DocumentEntry? existing = this.documents.FirstOrDefault( item => item.Identity is not null && this.pathComparer.Equals( item.Identity, identity ) );
		if ( existing is not null ) { this.Activate( existing.Key ); return existing; }
		DocumentEntry entry = this.Add( Path.GetFileName( fullPath ), fullPath, identity, loaded.Content.Encoding,
			loaded.Fingerprint.ToArray(), loaded.Content.Text );
		this.UpdateCaptions(); return entry;
	}

	internal bool Activate( string key, bool focusEditor = true ) {
		this.ThrowIfDisposed();
		DocumentEntry? next = this.documents.FirstOrDefault( document => document.Key == key );
		if ( next is null ) return false;
		if ( !ReferenceEquals( next, this.Active ) ) { this.Active?.Shell.Deactivate(); this.Active = next; }
		next.Shell.Activate( focusEditor ); return true;
	}

	private DocumentEntry Add( string caption, string? fullPath, string? identity, TextFileEncoding encoding, byte[] fingerprint, string text ) {
		this.ThrowIfDisposed();
		string key = Guid.NewGuid().ToString( "N", System.Globalization.CultureInfo.InvariantCulture );
		EditorShell shell = new( this.host, text, "editor-" + key, activate: false );
		DocumentEntry document = new( key, caption, fullPath, identity, encoding, fingerprint, shell );
		this.documents.Add( document ); this.Activate( key ); return document;
	}

	private void UpdateCaptions() {
		foreach ( IGrouping<string, DocumentEntry> group in this.documents.Where( item => item.FullPath is not null )
			.GroupBy( item => Path.GetFileName( item.FullPath! ), this.pathComparer ) ) {
			DocumentEntry[] entries = group.ToArray();
			if ( entries.Length == 1 ) { entries[ 0 ].Caption = group.Key; continue; }
			foreach ( DocumentEntry entry in entries ) {
				string parent = Path.GetDirectoryName( entry.FullPath! ) ?? "";
				string[] segments = parent.Split( [ Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ], StringSplitOptions.RemoveEmptyEntries );
				for ( int length = 1; length <= segments.Length; length++ ) {
					string suffix = string.Join( Path.DirectorySeparatorChar, segments.TakeLast( length ) );
					bool distinct = entries.Where( other => !ReferenceEquals( other, entry ) ).All( other => {
						string otherParent = Path.GetDirectoryName( other.FullPath! ) ?? "";
						string[] otherSegments = otherParent.Split( [ Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ], StringSplitOptions.RemoveEmptyEntries );
						return !this.pathComparer.Equals( suffix, string.Join( Path.DirectorySeparatorChar, otherSegments.TakeLast( length ) ) );
					} );
					if ( distinct || length == segments.Length ) { entry.Caption = group.Key + " — " + suffix; break; }
				}
			}
		}
	}

	private static string ResolveIdentity( string fullPath ) {
		try { return Path.GetFullPath( new FileInfo( fullPath ).ResolveLinkTarget( true )?.FullName ?? fullPath ); }
		catch ( IOException ) { return fullPath; }
		catch ( UnauthorizedAccessException ) { return fullPath; }
	}

	private void ThrowIfDisposed() { if ( this.disposed ) throw new ObjectDisposedException( nameof( DocumentManager ) ); }
	public void Dispose() {
		if ( this.disposed ) return;
		foreach ( DocumentEntry document in this.documents ) document.Shell.Dispose();
		this.documents.Clear(); this.Active = null; this.disposed = true;
	}
}
