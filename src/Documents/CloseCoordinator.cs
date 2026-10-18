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

using Icod.DCurses;
using Icod.Dedit.Presentation;
using Icod.Widgets;

internal enum CloseResolution { Unrelated, Kept, Closed, SaveAsRequired }

internal sealed class CloseCoordinator {
	private const string DialogId = "dedit-close-document";
	private const int DirtyDialogColumns = 45;
	private const int DirtyDialogRows = 5;
	private readonly WidgetHost host;
	private readonly DocumentManager documents;
	private readonly DocumentFileService files;
	private readonly WidgetStyles dirtyDialogStyles;
	private string? pendingKey;
	private string? pendingDialogId;
	private WidgetMessageDialog? pendingDialog;
	internal string? SaveAsKey { get; private set; }

	internal CloseCoordinator( WidgetHost host, DocumentManager documents, DocumentFileService files,
		WidgetStyles? dirtyDialogStyles = null ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.documents = documents ?? throw new ArgumentNullException( nameof( documents ) );
		this.files = files ?? throw new ArgumentNullException( nameof( files ) );
		this.dirtyDialogStyles = dirtyDialogStyles ?? WidgetStyles.Empty;
	}

	internal bool Request( string key ) {
		if ( this.pendingKey is not null ) return false;
		this.SaveAsKey = null;
		DocumentEntry? entry = this.documents.Find( key );
		if ( entry is null ) return false;
		if ( !entry.HasUnsavedChanges ) return this.documents.Close( key );
		this.pendingKey = key;
		this.pendingDialogId = DialogId + "-" + Guid.NewGuid().ToString( "N" );
		WidgetMessageDialog dialog = DeditMenu.CreateDirtyCloseDialog(
			this.pendingDialogId, entry.Caption, this.dirtyDialogStyles );
		this.pendingDialog = dialog;
		this.host.ShowModal( dialog );
		this.Arrange();
		return false;
	}

	internal void Arrange() {
		if ( this.pendingDialog is not { IsOpen: true } dialog ) return;
		CursesRectangle bounds = DirtyDialogBounds( this.host.Screen.Bounds );
		if ( dialog.Bounds != bounds ) dialog.Arrange( bounds );
	}

	private static CursesRectangle DirtyDialogBounds( CursesRectangle viewport ) {
		int rows = Math.Min( DirtyDialogRows, viewport.Rows );
		int columns = Math.Min( DirtyDialogColumns, viewport.Columns );
		return new CursesRectangle(
			viewport.Row + ( viewport.Rows - rows ) / 2,
			viewport.Column + ( viewport.Columns - columns ) / 2,
			rows,
			columns );
	}

	internal async Task<CloseResolution> CompleteAsync( WidgetDialogCompletedAction action, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( action );
		if ( action.WidgetId != this.pendingDialogId || this.pendingKey is null ) return CloseResolution.Unrelated;
		string key = this.pendingKey;
		this.pendingKey = null;
		this.pendingDialogId = null;
		this.pendingDialog = null;
		this.SaveAsKey = null;
		DocumentEntry? entry = this.documents.Find( key );
		if ( entry is null ) return CloseResolution.Kept;
		switch ( action.Result.Outcome ) {
			case WidgetDialogOutcome.No:
				return this.documents.Close( key, discardModified: true ) ? CloseResolution.Closed : CloseResolution.Kept;
			case WidgetDialogOutcome.Yes:
				if ( entry.FullPath is null ) { this.SaveAsKey = key; return CloseResolution.SaveAsRequired; }
				await this.documents.SaveAsync( entry, this.files, token );
				return !entry.HasUnsavedChanges && this.documents.Close( key )
					? CloseResolution.Closed : CloseResolution.Kept;
			default: return CloseResolution.Kept;
		}
	}
}
