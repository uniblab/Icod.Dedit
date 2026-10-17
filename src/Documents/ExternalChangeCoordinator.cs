/*
	Icod.Dedit
	Coordinates explicit reload and keep decisions for externally changed files.
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

internal enum ExternalDecisionResult { Unrelated, Unchanged, DecisionRequired, Kept, Canceled, Reloaded, Missing, Retargeted }

internal sealed class ExternalChangeCoordinator {
	private readonly WidgetHost host;
	private readonly DocumentManager documents;
	private readonly DocumentFileService files;
	private readonly Dictionary<string, byte[]> acknowledged = [];
	private string? pendingId;
	private DocumentEntry? pendingEntry;
	private byte[]? pendingFingerprint;

	internal ExternalChangeCoordinator( WidgetHost host, DocumentManager documents, DocumentFileService files ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.documents = documents ?? throw new ArgumentNullException( nameof( documents ) );
		this.files = files ?? throw new ArgumentNullException( nameof( files ) );
	}

	internal bool Matches( WidgetDialogCompletedAction action ) => action.WidgetId == this.pendingId && this.pendingEntry is not null;

	internal async Task<ExternalDecisionResult> CheckAsync( DocumentEntry entry, CancellationToken token ) {
		if ( this.pendingId is not null ) return ExternalDecisionResult.DecisionRequired;
		ExternalFileObservation? observation = await this.documents.CheckExternalChangeAsync( entry, this.files, token );
		if ( observation is null ) {
			this.acknowledged.Remove( entry.Key );
			return ExternalDecisionResult.Unchanged;
		}
		if ( observation.Kind == ExternalChangeKind.Missing ) return ExternalDecisionResult.Missing;
		if ( observation.Kind == ExternalChangeKind.Retargeted ) return ExternalDecisionResult.Retargeted;
		byte[] fingerprint = observation.Fingerprint!;
		if ( this.acknowledged.TryGetValue( entry.Key, out byte[]? seen ) && seen.AsSpan().SequenceEqual( fingerprint ) )
			return ExternalDecisionResult.Unchanged;
		string id = "dedit-external-change-" + Guid.NewGuid().ToString( "N" );
		this.host.ShowModal( DeditMenu.CreateExternalChangeDialog( id, entry.Caption, entry.HasUnsavedChanges ) );
		this.pendingId = id;
		this.pendingEntry = entry;
		this.pendingFingerprint = fingerprint;
		return ExternalDecisionResult.DecisionRequired;
	}

	internal async Task<ExternalDecisionResult> CompleteAsync( WidgetDialogCompletedAction action, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( action );
		if ( !this.Matches( action ) ) return ExternalDecisionResult.Unrelated;
		DocumentEntry entry = this.pendingEntry!;
		byte[] fingerprint = this.pendingFingerprint!;
		this.pendingId = null;
		this.pendingEntry = null;
		this.pendingFingerprint = null;
		if ( this.documents.Find( entry.Key ) != entry ) return ExternalDecisionResult.Unrelated;
		if ( action.Result.Outcome == WidgetDialogOutcome.Yes ) {
			await this.documents.ReloadAsync( entry, this.files, discardModified: true, token );
			this.acknowledged.Remove( entry.Key );
			return ExternalDecisionResult.Reloaded;
		}
		if ( action.Result.Outcome == WidgetDialogOutcome.No ) {
			this.acknowledged[ entry.Key ] = fingerprint;
			return ExternalDecisionResult.Kept;
		}
		return ExternalDecisionResult.Canceled;
	}
}
