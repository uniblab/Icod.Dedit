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
using Icod.Dedit.Configuration;
using Icod.Widgets;

internal sealed class EditorShell : IDisposable {
	private readonly WidgetHost host;
	private readonly WidgetTextViewState view;
	private bool disposed;

	internal EditorShell( WidgetHost host, string initialText = "", string editorId = "untitled-editor", bool activate = true, DeditPreferences? preferences = null ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		ArgumentNullException.ThrowIfNull( initialText );
		this.Document = new WidgetTextDocument( [ initialText.AsMemory() ] );
		this.view = new WidgetTextViewState( this.Document );
		DeditPreferences defaults = preferences ?? DeditPreferences.Default;
		this.IndentationWidth = defaults.IndentWidth;
		this.Editor = new WidgetTextEditor( editorId, this.view, new WidgetEditorOptions {
			Indentation = new string( ' ', defaults.IndentWidth ), NewLine = DetectNewLine( initialText ), TabWidth = defaults.TabWidth,
			WrapMode = defaults.Wrap ? CursesTextWrapMode.Word : CursesTextWrapMode.NoWrap,
			ShowLineNumbers = true, HighlightCurrentLine = true, ShowVerticalScrollBar = true,
			MaximumProjectionSourceUnits = 4_096
		} );
		if ( activate ) this.Activate();
	}

	internal WidgetTextDocument Document { get; }
	internal WidgetTextEditor Editor { get; }
	internal int IndentationWidth { get; private set; }

	internal void SetIndentationWidth( int width ) {
		if ( this.disposed ) throw new ObjectDisposedException( nameof( EditorShell ) );
		if ( width is < 1 or > 16 ) throw new ArgumentOutOfRangeException( nameof( width ) );
		this.Editor.Indentation = new string( ' ', width );
		this.IndentationWidth = width;
	}

	private static string DetectNewLine( string text ) {
		for ( int index = 0; index < text.Length; index++ ) {
			if ( text[ index ] == '\r' ) return index + 1 < text.Length && text[ index + 1 ] == '\n' ? "\r\n" : "\r";
			if ( text[ index ] == '\n' ) return "\n";
		}
		return "\n";
	}
	internal bool IsActive { get; private set; }

	internal void Activate( bool focus = true ) {
		if ( this.disposed ) throw new ObjectDisposedException( nameof( EditorShell ) );
		if ( !this.IsActive ) { this.host.Attach( this.Editor ); this.IsActive = true; }
		this.Arrange(); if ( focus ) this.host.Focus( this.Editor );
	}

	internal void Deactivate() {
		if ( !this.IsActive ) return;
		this.host.Detach( this.Editor ); this.IsActive = false;
	}

	internal void Arrange() {
		CursesScreen screen = this.host.Screen;
		CursesRectangle bounds = new( Math.Min( 2, screen.Rows ), 0,
			Math.Max( 0, screen.Rows - 2 - ( screen.Rows >= 4 ? 1 : 0 ) ), screen.Columns );
		if ( this.Editor.Bounds != bounds ) this.Editor.Arrange( bounds );
	}

	internal IReadOnlyList<WidgetAction> Process( WidgetInput input, TimeSpan elapsed ) {
		if ( input.Kind == WidgetInputKind.Key && input.KeyPhase is not CursesKeyEventPhase.Release &&
			( input.Modifiers & WidgetInputModifiers.Control ) != 0 && ReferenceEquals( this.host.FocusedWidget, this.Editor ) ) {
			WidgetEditCommand? command = input.Character?.Value switch {
				'z' or 'Z' => WidgetEditCommand.Undo, 'y' or 'Y' => WidgetEditCommand.Redo,
				'x' or 'X' => WidgetEditCommand.Cut, 'c' or 'C' => WidgetEditCommand.Copy,
				'v' or 'V' => WidgetEditCommand.Paste, 'a' or 'A' => WidgetEditCommand.SelectAll, _ => null
			};
			if ( command is not null ) { this.host.ExecuteEditCommand( command.Value, elapsed ); return this.host.DrainActions(); }
		}
		return this.host.Process( input, elapsed ).Actions;
	}

	public void Dispose() {
		if ( this.disposed ) return;
		this.Deactivate(); this.view.Dispose(); this.disposed = true;
	}
}
