/*
	Icod.Dedit
	Hosts one in-memory text document and its editor view.
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

namespace Icod.Dedit.Presentation;

using Icod.DCurses;
using Icod.Widgets;

internal sealed class EditorShell : IDisposable {
	private readonly WidgetHost host;
	private readonly WidgetTextViewState view;
	private bool disposed;

	internal EditorShell( WidgetHost host ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.Document = new WidgetTextDocument( [ ReadOnlyMemory<char>.Empty ] );
		this.view = new WidgetTextViewState( this.Document );
		this.Editor = new WidgetTextEditor( "untitled-editor", this.view, new WidgetEditorOptions {
			Indentation = "    ", TabWidth = 4, MaximumProjectionSourceUnits = 4_096
		} );
		this.host.Attach( this.Editor );
		this.Arrange(); this.host.Focus( this.Editor );
	}

	internal WidgetTextDocument Document { get; }
	internal WidgetTextEditor Editor { get; }

	internal void Arrange() {
		CursesScreen screen = this.host.Screen;
		this.Editor.Arrange( new CursesRectangle( Math.Min( 2, screen.Rows ), 0,
			Math.Max( 0, screen.Rows - 2 - ( screen.Rows >= 4 ? 1 : 0 ) ), screen.Columns ) );
	}

	internal IReadOnlyList<WidgetAction> Process( WidgetInput input, TimeSpan elapsed ) {
		if ( input.Kind == WidgetInputKind.Key && input.KeyPhase is not CursesKeyEventPhase.Release &&
			( input.Modifiers & WidgetInputModifiers.Control ) != 0 && ReferenceEquals( this.host.FocusedWidget, this.Editor ) ) {
			WidgetEditCommand? command = input.Character?.Value switch { 'z' or 'Z' => WidgetEditCommand.Undo, 'y' or 'Y' => WidgetEditCommand.Redo, _ => null };
			if ( command is not null ) { this.host.ExecuteEditCommand( command.Value, elapsed ); return this.host.DrainActions(); }
		}
		return this.host.Process( input, elapsed ).Actions;
	}

	public void Dispose() {
		if ( this.disposed ) return;
		this.host.Detach( this.Editor ); this.view.Dispose(); this.disposed = true;
	}
}
