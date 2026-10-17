/*
	Icod.Dedit
	Binds a validated Go to Line form to one document revision.
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

namespace Icod.Dedit.Interaction;

using Icod.DCurses;
using Icod.Dedit.Documents;
using Icod.Widgets;

internal sealed class DeditGoToCoordinator( WidgetHost host, DocumentManager documents ) {
	private WidgetFormDialog? form;
	private DocumentEntry? entry;
	private long revision;
	private long? pendingOffset;
	internal WidgetTextInput? Input { get; private set; }

	internal WidgetFormDialog Open( DocumentEntry target ) {
		ArgumentNullException.ThrowIfNull( target );
		if ( documents.Find( target.Key ) != target ) throw new ArgumentException( "Document is not open.", nameof( target ) );
		if ( this.form?.IsOpen == true ) throw new InvalidOperationException( "Go to Line is already open." );
		string id = "dedit-go-to-" + Guid.NewGuid().ToString( "N" );
		WidgetTextInput input = new( id + ".coordinate" );
		WidgetFormDialog next = new( id, "Go to Line:Column", [
			new WidgetDialogAction( "go", "Go", WidgetDialogOutcome.Accepted ) { IsDefault = true, RequiresValidation = true },
			new WidgetDialogAction( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsCancel = true }
		], input.Id );
		next.Add( input );
		this.entry = target; this.revision = target.Shell.Document.Revision;
		this.form = next; this.Input = input; this.pendingOffset = null;
		host.ShowModal( next );
		this.Arrange();
		return next;
	}

	internal void Arrange() {
		if ( this.form is not { IsOpen: true } active || this.Input is not { } input ) return;
		CursesRectangle area = active.Bounds.Intersect( host.Screen.Bounds );
		input.Arrange( new CursesRectangle( area.Row + Math.Min( 1, area.Rows ), area.Column + Math.Min( 1, area.Columns ),
			Math.Min( 1, Math.Max( 0, area.Rows - 2 ) ), Math.Max( 0, area.Columns - 2 ) ) );
	}

	internal bool Validate( WidgetDialogValidationRequestedAction action ) {
		if ( this.form is not { IsOpen: true } active || action.WidgetId != active.Id ) return false;
		DocumentEntry? target = this.entry;
		bool current = target is not null && documents.Find( target.Key ) == target &&
			target.Shell.Document.Revision == this.revision;
		long offset = 0;
		bool valid = current && this.Input is not null &&
			GoToCoordinate.TryResolve( target!.Shell.Document, this.Input.Value, out offset );
		this.pendingOffset = valid ? offset : null;
		active.ApplyValidationResult( new WidgetDialogValidationResult( action.Request, valid,
			current ? "Enter an existing line:column (both start at 1)." : "Document changed; reopen Go to Line." ) );
		return true;
	}

	internal bool Complete( WidgetDialogCompletedAction action ) {
		if ( this.form is null || action.WidgetId != this.form.Id ) return false;
		DocumentEntry? target = this.entry;
		if ( action.Result.Outcome == WidgetDialogOutcome.Accepted && this.pendingOffset is { } offset &&
			target is not null && documents.Find( target.Key ) == target &&
			target.Shell.Document.Revision == this.revision && target.Shell.Document.IsTextElementBoundary( offset ) )
			target.Shell.Editor.View.SetSelection( offset, offset );
		this.form = null; this.Input = null; this.entry = null; this.pendingOffset = null;
		return true;
	}
}
