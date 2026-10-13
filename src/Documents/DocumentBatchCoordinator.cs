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
	internal DocumentBatchCoordinator( DocumentManager documents, DocumentFileService files, CloseCoordinator close ) {
		throw new NotImplementedException();
	}
	internal string? SaveAsKey => throw new NotImplementedException();
	internal Task<BatchProgress> StartAsync( BatchOperation operation, CancellationToken token ) => throw new NotImplementedException();
	internal Task<BatchProgress> CompleteDecisionAsync( WidgetDialogCompletedAction action, CancellationToken token ) => throw new NotImplementedException();
	internal Task<BatchProgress> CompleteSaveAsAsync( bool saved, CancellationToken token ) => throw new NotImplementedException();
	internal void Abort() => throw new NotImplementedException();
}
