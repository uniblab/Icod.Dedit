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
	internal const string Save = "file.save";
	internal const string GoToFile = "view.go-file";
	internal const string Wrap = "view.wrap";
	internal const string About = "help.about";

	internal static WidgetMenuBar Create() => new( "dedit-menu", [
		WidgetMenuItem.Submenu( "edit", "Edit", [
			WidgetMenuItem.Command( "edit.undo", "Undo", "edit.undo", 'u', WidgetEditCommand.Undo,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'z' ) ),
			WidgetMenuItem.Command( "edit.redo", "Redo", "edit.redo", 'r', WidgetEditCommand.Redo,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'y' ) ),
			WidgetMenuItem.Separator( "edit.clipboard-separator" ),
			WidgetMenuItem.Command( "edit.cut", "Cut", "edit.cut", 't', WidgetEditCommand.Cut,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'x' ) ),
			WidgetMenuItem.Command( "edit.copy", "Copy", "edit.copy", 'c', WidgetEditCommand.Copy,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'c' ) ),
			WidgetMenuItem.Command( "edit.paste", "Paste", "edit.paste", 'p', WidgetEditCommand.Paste,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'v' ) ),
			WidgetMenuItem.Command( "edit.select-all", "Select All", "edit.select-all", 'a', WidgetEditCommand.SelectAll,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'a' ) )
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

	internal static void UpdateState( WidgetMenuBar menu, WidgetTextEditor? editor, bool canSave = false ) {
		ArgumentNullException.ThrowIfNull( menu );
		bool undo = editor?.CanExecuteEditCommand( WidgetEditCommand.Undo ) == true;
		bool redo = editor?.CanExecuteEditCommand( WidgetEditCommand.Redo ) == true;
		bool cut = editor?.CanExecuteEditCommand( WidgetEditCommand.Cut ) == true;
		bool copy = editor?.CanExecuteEditCommand( WidgetEditCommand.Copy ) == true;
		bool paste = editor?.CanExecuteEditCommand( WidgetEditCommand.Paste ) == true;
		bool selectAll = editor?.CanExecuteEditCommand( WidgetEditCommand.SelectAll ) == true;
		menu.SetPresentation( [
			new WidgetMenuItemPresentation( "edit", "Edit", undo || redo || cut || copy || paste || selectAll ),
			new WidgetMenuItemPresentation( "edit.undo", "Undo", undo ),
			new WidgetMenuItemPresentation( "edit.redo", "Redo", redo ),
			new WidgetMenuItemPresentation( "edit.cut", "Cut", cut ),
			new WidgetMenuItemPresentation( "edit.copy", "Copy", copy ),
			new WidgetMenuItemPresentation( "edit.paste", "Paste", paste ),
			new WidgetMenuItemPresentation( "edit.select-all", "Select All", selectAll ),
			new WidgetMenuItemPresentation( "view.wrap", "Word Wrap", editor is not null,
				editor is not null && editor.View.WrapMode != CursesTextWrapMode.NoWrap )
		] );
	}

	internal static void ApplyWrap( WidgetTextEditor editor, bool enabled ) {
		ArgumentNullException.ThrowIfNull( editor );
		editor.View.WrapMode = enabled ? CursesTextWrapMode.Word : CursesTextWrapMode.NoWrap;
	}
}
