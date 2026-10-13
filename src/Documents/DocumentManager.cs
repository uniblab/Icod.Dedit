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
using Icod.Dedit.Configuration;
using Icod.Widgets;

internal enum ExternalChangeKind { Modified, Missing, Retargeted }
internal sealed record ExternalFileObservation( ExternalChangeKind Kind, byte[]? Fingerprint );

internal sealed class DocumentEntry( string key, string caption, string? fullPath, string? identity, TextFileEncoding encoding,
	byte[] fingerprint, EditorShell shell ) {
	internal string Key { get; } = key;
	internal string Caption { get; set; } = caption;
	internal string? FullPath { get; set; } = fullPath;
	internal string? Identity { get; set; } = identity;
	internal TextFileEncoding Encoding { get; set; } = encoding;
	internal TextFileEncoding SavedEncoding { get; set; } = encoding;
	internal bool HasUnsavedChanges => this.Shell.Document.IsModified || this.Encoding != this.SavedEncoding;
	internal byte[] Fingerprint { get; set; } = fingerprint;
	internal EditorShell Shell { get; set; } = shell;
}

internal sealed class DocumentManager : IDisposable {
	private readonly WidgetHost host;
	private readonly DeditPreferences preferences;
	private readonly List<DocumentEntry> documents = [];
	private readonly StringComparer pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
	private int nextUntitled;
	private bool disposed;

	internal DocumentManager( WidgetHost host, DeditPreferences? preferences = null ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.preferences = preferences ?? DeditPreferences.Default;
	}
	internal DocumentEntry? Active { get; private set; }
	internal IReadOnlyList<WidgetTab> Tabs => this.documents.Select( document => new WidgetTab( document.Key, SanitizeDisplay( document.Caption ),
		IsModified: document.HasUnsavedChanges, IsClosable: true ) ).ToArray();
	internal static string SanitizeDisplay( string text ) => new( text.Select( character => char.IsControl( character ) ? '�' : character ).ToArray() );

	internal DocumentEntry OpenUntitled() => this.Add( $"Untitled{( ++this.nextUntitled == 1 ? "" : $" {this.nextUntitled}" )}", null, null,
		this.preferences.DefaultEncoding, [], "" );

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

	internal void SetEncoding( DocumentEntry entry, TextFileEncoding encoding ) {
		ArgumentNullException.ThrowIfNull( entry );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		if ( !Enum.IsDefined( encoding ) ) throw new ArgumentOutOfRangeException( nameof( encoding ) );
		entry.Encoding = encoding;
	}

	internal async Task SaveAsNewAsync( DocumentEntry entry, DocumentFileService files, string path,
		CancellationToken cancellationToken ) {
		ArgumentNullException.ThrowIfNull( entry );
		ArgumentNullException.ThrowIfNull( files );
		ArgumentException.ThrowIfNullOrWhiteSpace( path );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		string fullPath = files.ResolvePath( path );
		string identity = ResolveIdentity( fullPath );
		if ( this.documents.Any( other => !ReferenceEquals( other, entry ) &&
			other.Identity is not null && this.pathComparer.Equals( other.Identity, identity ) ) )
			throw new IOException( "The save target belongs to another open tab." );
		using WidgetTextSnapshot snapshot = entry.Shell.Document.CreateSnapshot();
		TextFileEncoding selectedEncoding = entry.Encoding;
		string text = snapshot.Read( 0, checked( (int)snapshot.Length ) );
		byte[] fingerprint = await files.SaveNewAsync( fullPath, text, selectedEncoding, cancellationToken );
		entry.FullPath = fullPath; entry.Identity = identity; entry.Fingerprint = fingerprint;
		entry.SavedEncoding = selectedEncoding;
		this.UpdateCaptions();
		entry.Shell.Document.MarkSaved( snapshot );
	}

	internal async Task SaveAsExistingAsync( DocumentEntry entry, DocumentFileService files, ExistingSaveTarget target,
		CancellationToken cancellationToken ) {
		ArgumentNullException.ThrowIfNull( entry );
		ArgumentNullException.ThrowIfNull( files );
		ArgumentNullException.ThrowIfNull( target );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		string fullPath = files.ResolvePath( target.FullPath );
		string identity = ResolveIdentity( fullPath );
		if ( !this.pathComparer.Equals( identity, target.Identity ) )
			throw new IOException( "The save target changed during confirmation." );
		if ( this.documents.Any( other => !ReferenceEquals( other, entry ) &&
			other.Identity is not null && this.pathComparer.Equals( other.Identity, identity ) ) )
			throw new IOException( "The save target belongs to another open tab." );
		byte[] baseline = entry.Identity is not null && this.pathComparer.Equals( entry.Identity, identity )
			? entry.Fingerprint : target.Fingerprint;
		using WidgetTextSnapshot snapshot = entry.Shell.Document.CreateSnapshot();
		TextFileEncoding selectedEncoding = entry.Encoding;
		string text = snapshot.Read( 0, checked( (int)snapshot.Length ) );
		byte[] fingerprint = await files.SaveAsync( fullPath, target.Identity, baseline, text, selectedEncoding, cancellationToken );
		entry.FullPath = fullPath; entry.Identity = identity; entry.Fingerprint = fingerprint;
		entry.SavedEncoding = selectedEncoding;
		this.UpdateCaptions();
		entry.Shell.Document.MarkSaved( snapshot );
	}

	internal async Task SaveAsync( DocumentEntry entry, DocumentFileService files, CancellationToken cancellationToken ) {
		ArgumentNullException.ThrowIfNull( entry );
		ArgumentNullException.ThrowIfNull( files );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		if ( entry.FullPath is null || entry.Identity is null ) throw new InvalidOperationException( "Untitled documents require Save As." );
		using WidgetTextSnapshot snapshot = entry.Shell.Document.CreateSnapshot();
		TextFileEncoding selectedEncoding = entry.Encoding;
		string text = snapshot.Read( 0, checked( (int)snapshot.Length ) );
		byte[] fingerprint = await files.SaveAsync( entry.FullPath, entry.Identity, entry.Fingerprint, text, selectedEncoding, cancellationToken );
		entry.Fingerprint = fingerprint;
		entry.SavedEncoding = selectedEncoding;
		entry.Shell.Document.MarkSaved( snapshot );
	}

	internal async Task<ExternalFileObservation?> CheckExternalChangeAsync( DocumentEntry entry, DocumentFileService files,
		CancellationToken cancellationToken ) {
		ArgumentNullException.ThrowIfNull( entry );
		ArgumentNullException.ThrowIfNull( files );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		if ( entry.FullPath is null || entry.Identity is null ) return null;
		ExistingSaveTarget target;
		try { target = await files.CaptureExistingAsync( entry.FullPath, cancellationToken ); }
		catch ( FileNotFoundException ) { return new ExternalFileObservation( ExternalChangeKind.Missing, null ); }
		catch ( DirectoryNotFoundException ) { return new ExternalFileObservation( ExternalChangeKind.Missing, null ); }
		if ( !this.pathComparer.Equals( target.Identity, entry.Identity ) )
			return new ExternalFileObservation( ExternalChangeKind.Retargeted, target.Fingerprint );
		if ( target.Fingerprint.AsSpan().SequenceEqual( entry.Fingerprint ) ) return null;
		return new ExternalFileObservation( ExternalChangeKind.Modified, target.Fingerprint );
	}

	internal async Task ReloadAsync( DocumentEntry entry, DocumentFileService files, bool discardModified,
		CancellationToken cancellationToken ) {
		ArgumentNullException.ThrowIfNull( entry );
		ArgumentNullException.ThrowIfNull( files );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		if ( entry.FullPath is null || entry.Identity is null ) throw new InvalidOperationException( "Untitled documents cannot reload." );
		EditorShell previous = entry.Shell;
		if ( entry.HasUnsavedChanges && !discardModified )
			throw new InvalidOperationException( "Reloading would discard unsaved changes." );
		long revision = previous.Document.Revision;
		string path = entry.FullPath;
		string identity = entry.Identity;
		LoadedTextFile loaded = await files.OpenAsync( path, cancellationToken );
		this.ThrowIfDisposed();
		if ( !this.documents.Contains( entry ) || !ReferenceEquals( entry.Shell, previous ) ||
			previous.Document.Revision != revision || ( entry.HasUnsavedChanges && !discardModified ) ||
			!this.pathComparer.Equals( entry.FullPath, path ) || !this.pathComparer.Equals( entry.Identity, identity ) )
			throw new IOException( "The document changed during reload." );
		if ( !this.pathComparer.Equals( ResolveIdentity( path ), identity ) )
			throw new IOException( "The file target changed during reload." );
		EditorShell replacement = new( this.host, loaded.Content.Text, "editor-" + entry.Key, activate: false, this.preferences );
		replacement.Editor.View.WrapMode = previous.Editor.View.WrapMode;
		replacement.Editor.View.TabWidth = previous.Editor.View.TabWidth;
		bool wasActive = ReferenceEquals( this.Active, entry );
		bool wasFocused = wasActive && ReferenceEquals( this.host.FocusedWidget, previous.Editor );
		try {
			if ( wasActive ) {
				previous.Deactivate();
				replacement.Activate( wasFocused );
			}
			entry.Shell = replacement;
			entry.Encoding = loaded.Content.Encoding;
			entry.SavedEncoding = loaded.Content.Encoding;
			entry.Fingerprint = loaded.Fingerprint;
			previous.Dispose();
		} catch {
			replacement.Dispose();
			if ( wasActive ) previous.Activate( wasFocused );
			throw;
		}
	}

	internal DocumentEntry? Find( string key ) {
		this.ThrowIfDisposed();
		return this.documents.FirstOrDefault( document => document.Key == key );
	}

	internal bool Close( string key, bool discardModified = false ) {
		this.ThrowIfDisposed();
		int index = this.documents.FindIndex( document => document.Key == key );
		if ( index < 0 ) return false;
		DocumentEntry closing = this.documents[ index ];
		if ( closing.HasUnsavedChanges && !discardModified ) return false;
		bool wasActive = ReferenceEquals( closing, this.Active );
		if ( wasActive ) { closing.Shell.Deactivate(); this.Active = null; }
		this.documents.RemoveAt( index );
		closing.Shell.Dispose();
		if ( wasActive ) {
			if ( this.documents.Count == 0 ) this.OpenUntitled();
			else this.Activate( this.documents[ Math.Min( index, this.documents.Count - 1 ) ].Key );
		}
		this.UpdateCaptions();
		return true;
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
		EditorShell shell = new( this.host, text, "editor-" + key, activate: false, this.preferences );
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
		try { return PathIdentity.Resolve( fullPath ); }
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
