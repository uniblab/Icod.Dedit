/*
	Icod.Dedit
	Coordinates safe document close decisions.
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

internal enum CloseResolution { Unrelated, Kept, Closed, SaveAsRequired }

internal sealed class CloseCoordinator {
	private const string DialogId = "dedit-close-document";
	private readonly WidgetHost host;
	private readonly DocumentManager documents;
	private readonly DocumentFileService files;
	private string? pendingKey;
	private string? pendingDialogId;
	internal string? SaveAsKey { get; private set; }

	internal CloseCoordinator( WidgetHost host, DocumentManager documents, DocumentFileService files ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.documents = documents ?? throw new ArgumentNullException( nameof( documents ) );
		this.files = files ?? throw new ArgumentNullException( nameof( files ) );
	}

	internal bool Request( string key ) {
		if ( this.pendingKey is not null ) return false;
		this.SaveAsKey = null;
		DocumentEntry? entry = this.documents.Find( key );
		if ( entry is null ) return false;
		if ( !entry.Shell.Document.IsModified ) return this.documents.Close( key );
		this.pendingKey = key;
		this.pendingDialogId = DialogId + "-" + Guid.NewGuid().ToString( "N" );
		this.host.ShowModal( WidgetFormDialog.SaveDontSaveCancel( this.pendingDialogId, "Unsaved changes",
			$"Save changes in {DocumentManager.SanitizeDisplay( entry.Caption )} before closing?" ) );
		return false;
	}

	internal async Task<CloseResolution> CompleteAsync( WidgetDialogCompletedAction action, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( action );
		if ( action.WidgetId != this.pendingDialogId || this.pendingKey is null ) return CloseResolution.Unrelated;
		string key = this.pendingKey;
		this.pendingKey = null;
		this.pendingDialogId = null;
		this.SaveAsKey = null;
		DocumentEntry? entry = this.documents.Find( key );
		if ( entry is null ) return CloseResolution.Kept;
		switch ( action.Result.Outcome ) {
			case WidgetDialogOutcome.DontSave:
				return this.documents.Close( key, discardModified: true ) ? CloseResolution.Closed : CloseResolution.Kept;
			case WidgetDialogOutcome.Save:
				if ( entry.FullPath is null ) { this.SaveAsKey = key; return CloseResolution.SaveAsRequired; }
				await this.documents.SaveAsync( entry, this.files, token );
				return !entry.Shell.Document.IsModified && this.documents.Close( key )
					? CloseResolution.Closed : CloseResolution.Kept;
			default: return CloseResolution.Kept;
		}
	}
}
