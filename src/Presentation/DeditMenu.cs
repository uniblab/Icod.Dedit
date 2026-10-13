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

using Icod.Widgets;

internal static class DeditMenu {
	internal const string GoToFile = "view.go-file";

	internal static WidgetMenuBar Create() => new( "dedit-menu", [
		WidgetMenuItem.Submenu( "edit", "Edit", [
			WidgetMenuItem.Command( "edit.undo", "Undo", "edit.undo", 'u', WidgetEditCommand.Undo,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'z' ) ),
			WidgetMenuItem.Command( "edit.redo", "Redo", "edit.redo", 'r', WidgetEditCommand.Redo,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'y' ) )
		], 'e' ),
		WidgetMenuItem.Submenu( "view", "View", [
			WidgetMenuItem.Command( "view.go-file", "Go to File...", GoToFile, 'f',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'p' ) )
		], 'v' )
	] );

	internal static void UpdateEditState( WidgetMenuBar menu, WidgetTextEditor? editor ) {
		ArgumentNullException.ThrowIfNull( menu );
		bool undo = editor?.CanExecuteEditCommand( WidgetEditCommand.Undo ) == true;
		bool redo = editor?.CanExecuteEditCommand( WidgetEditCommand.Redo ) == true;
		menu.SetPresentation( [
			new WidgetMenuItemPresentation( "edit", "Edit", undo || redo ),
			new WidgetMenuItemPresentation( "edit.undo", "Undo", undo ),
			new WidgetMenuItemPresentation( "edit.redo", "Redo", redo )
		] );
	}
}
