/*
	Icod.Dedit
	Defines application menu navigation independently of direct shortcuts.
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

internal static class DeditMenu {
	internal const string GoToFile = "view.go-file";
	internal const string Wrap = "view.wrap";
	internal const string About = "help.about";

	internal static WidgetMenuBar Create() => new( "dedit-menu", [
		WidgetMenuItem.Submenu( "edit", "Edit", [
			WidgetMenuItem.Command( "edit.undo", "Undo", "edit.undo", 'u', WidgetEditCommand.Undo,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'z' ) ),
			WidgetMenuItem.Command( "edit.redo", "Redo", "edit.redo", 'r', WidgetEditCommand.Redo,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'y' ) )
		], 'e' ),
		WidgetMenuItem.Submenu( "view", "View", [
			WidgetMenuItem.Command( "view.go-file", "Go to File...", GoToFile, 'f',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'p' ) ),
			WidgetMenuItem.Checkable( "view.wrap", "Word Wrap", Wrap, 'w',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Alt, 'z' ) )
		], 'v' ),
		WidgetMenuItem.Submenu( "help", "Help", [
			WidgetMenuItem.Command( "help.about", "About dedit", About, 'a' )
		], 'h' )
	] );

	internal static WidgetMessageDialog CreateAboutDialog() => WidgetMessageDialog.Ok(
		"dedit-about", "About dedit", $"dedit {typeof( DeditMenu ).Assembly.GetName().Version?.ToString( 3 ) ?? "0.1.0"} — GPL-3.0-or-later" );

	internal static void UpdateState( WidgetMenuBar menu, WidgetTextEditor? editor ) {
		ArgumentNullException.ThrowIfNull( menu );
		bool undo = editor?.CanExecuteEditCommand( WidgetEditCommand.Undo ) == true;
		bool redo = editor?.CanExecuteEditCommand( WidgetEditCommand.Redo ) == true;
		menu.SetPresentation( [
			new WidgetMenuItemPresentation( "edit", "Edit", undo || redo ),
			new WidgetMenuItemPresentation( "edit.undo", "Undo", undo ),
			new WidgetMenuItemPresentation( "edit.redo", "Redo", redo ),
			new WidgetMenuItemPresentation( "view.wrap", "Word Wrap", editor is not null,
				editor is not null && editor.View.WrapMode != CursesTextWrapMode.NoWrap )
		] );
	}

	internal static void ApplyWrap( WidgetTextEditor editor, bool enabled ) {
		ArgumentNullException.ThrowIfNull( editor );
		editor.View.WrapMode = enabled ? CursesTextWrapMode.Word : CursesTextWrapMode.NoWrap;
	}
}
