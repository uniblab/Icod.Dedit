/*
	Icod.Dedit
	Coordinates document-wide save and close operations.
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

internal enum BatchOperation { SaveAll, CloseAll, Exit }
internal enum BatchProgress { Stopped, DecisionRequired, SaveAsRequired, Completed, ExitReady }

internal sealed class DocumentBatchCoordinator {
	private readonly DocumentManager documents;
	private readonly DocumentFileService files;
	private readonly CloseCoordinator close;
	private string[] keys = [];
	private int index;
	private BatchOperation? operation;
	private bool awaitingDecision;
	private bool awaitingSaveAs;

	internal DocumentBatchCoordinator( DocumentManager documents, DocumentFileService files, CloseCoordinator close ) {
		this.documents = documents ?? throw new ArgumentNullException( nameof( documents ) );
		this.files = files ?? throw new ArgumentNullException( nameof( files ) );
		this.close = close ?? throw new ArgumentNullException( nameof( close ) );
	}

	internal bool IsActive => this.operation is not null;
	internal string? SaveAsKey => this.awaitingSaveAs && this.index < this.keys.Length ? this.keys[ this.index ] : null;

	internal async Task<BatchProgress> StartAsync( BatchOperation operation, CancellationToken token ) {
		if ( this.IsActive ) throw new InvalidOperationException( "A document operation is already active." );
		this.keys = this.documents.Tabs.Select( tab => tab.Key ).ToArray();
		this.index = 0;
		this.operation = operation;
		try { return await this.AdvanceAsync( token ); }
		catch { this.Abort(); throw; }
	}

	internal async Task<BatchProgress> CompleteDecisionAsync( WidgetDialogCompletedAction action, CancellationToken token ) {
		if ( !this.awaitingDecision ) return BatchProgress.Stopped;
		CloseResolution resolution;
		try { resolution = await this.close.CompleteAsync( action, token ); }
		catch { this.Abort(); throw; }
		if ( resolution == CloseResolution.Unrelated ) return BatchProgress.DecisionRequired;
		this.awaitingDecision = false;
		if ( resolution == CloseResolution.Kept ) { this.Abort(); return BatchProgress.Stopped; }
		if ( resolution == CloseResolution.SaveAsRequired ) {
			this.awaitingSaveAs = true;
			return BatchProgress.SaveAsRequired;
		}
		this.index++;
		return await this.AdvanceAsync( token );
	}

	internal async Task<BatchProgress> CompleteSaveAsAsync( bool saved, CancellationToken token ) {
		if ( !this.awaitingSaveAs ) return BatchProgress.Stopped;
		this.awaitingSaveAs = false;
		DocumentEntry? entry = this.documents.Find( this.keys[ this.index ] );
		if ( !saved || entry is null || entry.HasUnsavedChanges ) {
			this.Abort(); return BatchProgress.Stopped;
		}
		if ( this.operation != BatchOperation.SaveAll && !this.documents.Close( entry.Key ) ) {
			this.Abort(); return BatchProgress.Stopped;
		}
		this.index++;
		return await this.AdvanceAsync( token );
	}

	internal void Abort() {
		this.keys = [];
		this.index = 0;
		this.operation = null;
		this.awaitingDecision = false;
		this.awaitingSaveAs = false;
	}

	private async Task<BatchProgress> AdvanceAsync( CancellationToken token ) {
		while ( this.index < this.keys.Length ) {
			DocumentEntry? entry = this.documents.Find( this.keys[ this.index ] );
			if ( entry is null ) { this.index++; continue; }
			if ( this.operation == BatchOperation.SaveAll ) {
				if ( !entry.HasUnsavedChanges ) { this.index++; continue; }
				if ( entry.FullPath is null ) {
					this.awaitingSaveAs = true;
					return BatchProgress.SaveAsRequired;
				}
				await this.documents.SaveAsync( entry, this.files, token );
				if ( entry.HasUnsavedChanges ) {
					this.Abort(); return BatchProgress.Stopped;
				}
				this.index++;
			} else {
				if ( this.close.Request( entry.Key ) ) this.index++;
				else {
					this.awaitingDecision = true;
					return BatchProgress.DecisionRequired;
				}
			}
		}
		bool exit = this.operation == BatchOperation.Exit;
		this.Abort();
		return exit ? BatchProgress.ExitReady : BatchProgress.Completed;
	}
}
