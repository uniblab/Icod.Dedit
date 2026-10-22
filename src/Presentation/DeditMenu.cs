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
using Icod.Dedit.Documents;
using Icod.Widgets;

internal static class DeditMenu {
	internal const string New = "file.new";
	internal const string Open = "file.open";
	internal const string Save = "file.save";
	internal const string SaveAs = "file.save-as";
	internal const string SaveAll = "file.save-all";
	internal const string CloseAll = "file.close-all";
	internal const string Exit = "file.exit";
	internal const string Close = "file.close";
	internal const string Find = "edit.find";
	internal const string Replace = "edit.replace";
	internal const string FindNext = "edit.find-next";
	internal const string FindPrevious = "edit.find-previous";
	internal const string SearchCase = "edit.search-case";
	internal const string SearchWholeWord = "edit.search-whole-word";
	internal const string SearchRegex = "edit.search-regex";
	internal const string IndentTwo = "file.indentation.two";
	internal const string IndentFour = "file.indentation.four";
	internal const string IndentEight = "file.indentation.eight";
	internal const string LineEndingLf = "file.line-endings.lf";
	internal const string LineEndingCrLf = "file.line-endings.crlf";
	internal const string LineEndingCr = "file.line-endings.cr";
	internal const string EncodingUtf8 = "file.encoding.utf8";
	internal const string EncodingUtf8Bom = "file.encoding.utf8-bom";
	internal const string EncodingUtf16Le = "file.encoding.utf16-le";
	internal const string EncodingUtf16Be = "file.encoding.utf16-be";
	internal const string GoToFile = "view.go-file";
	internal const string GoToLine = "view.go-line";
	internal const string Wrap = "view.wrap";
	internal const string About = "help.about";

	internal static WidgetMenuBar Create() => new( "dedit-menu", [
		WidgetMenuItem.Submenu( "file", "File", [
			WidgetMenuItem.Command( "file.new", "New", New, 'n',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'n' ) ),
			WidgetMenuItem.Command( "file.open", "Open...", Open, 'o',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'o' ) ),
			WidgetMenuItem.Command( "file.save", "Save", Save, 's',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 's' ) ),
			WidgetMenuItem.Command( "file.save-as", "Save As...", SaveAs, 'a',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control | WidgetInputModifiers.Shift, 's' ) ),
			WidgetMenuItem.Command( "file.save-all", "Save All", SaveAll, 'l' ),
			WidgetMenuItem.Submenu( "file.encoding", "Encoding", [
				WidgetMenuItem.Checkable( "file.encoding.utf8", "UTF-8", EncodingUtf8, 'u' ),
				WidgetMenuItem.Checkable( "file.encoding.utf8-bom", "UTF-8 BOM", EncodingUtf8Bom, 'b' ),
				WidgetMenuItem.Checkable( "file.encoding.utf16-le", "UTF-16 LE", EncodingUtf16Le, 'l' ),
				WidgetMenuItem.Checkable( "file.encoding.utf16-be", "UTF-16 BE", EncodingUtf16Be, 'e' )
			], 'e' ),
			WidgetMenuItem.Submenu( "file.line-endings", "Line Endings", [
				WidgetMenuItem.Command( "file.line-endings.lf", "Unix LF", LineEndingLf, 'u' ),
				WidgetMenuItem.Command( "file.line-endings.crlf", "Windows CRLF", LineEndingCrLf, 'w' ),
				WidgetMenuItem.Command( "file.line-endings.cr", "Classic Mac CR", LineEndingCr, 'c' )
			], 't' ),
			WidgetMenuItem.Submenu( "file.indentation", "Indentation", [
				WidgetMenuItem.Checkable( "file.indentation.two", "2 Spaces", IndentTwo, '2' ),
				WidgetMenuItem.Checkable( "file.indentation.four", "4 Spaces", IndentFour, '4' ),
				WidgetMenuItem.Checkable( "file.indentation.eight", "8 Spaces", IndentEight, '8' )
			], 'i' ),
			WidgetMenuItem.Command( "file.close", "Close", Close, 'c',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'w' ) ),
			WidgetMenuItem.Command( "file.close-all", "Close All", CloseAll, 'd' ),
			WidgetMenuItem.Command( "file.exit", "Exit", Exit, 'x' )
		], 'f' ),
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
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'a' ) ),
			WidgetMenuItem.Separator( "edit.search-separator" ),
			WidgetMenuItem.Command( "edit.find", "Find...", Find, 'f',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'f' ) ),
			WidgetMenuItem.Command( "edit.replace", "Replace...", Replace, 'l',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'r' ) ),
			WidgetMenuItem.Command( "edit.find-next", "Find Next", FindNext, 'n',
				shortcut: new WidgetMenuShortcut( WidgetKey.Function ) { FunctionKeyNumber = 3 } ),
			WidgetMenuItem.Command( "edit.find-previous", "Find Previous", FindPrevious, 'v',
				shortcut: new WidgetMenuShortcut( WidgetKey.Function, WidgetInputModifiers.Shift ) { FunctionKeyNumber = 3 } ),
			WidgetMenuItem.Separator( "edit.options-separator" ),
			WidgetMenuItem.Checkable( "edit.search-case", "Match Case", SearchCase, 'm' ),
			WidgetMenuItem.Checkable( "edit.search-whole-word", "Whole Word", SearchWholeWord, 'w' ),
			WidgetMenuItem.Checkable( "edit.search-regex", "Regular Expression", SearchRegex, 'g' )
		], 'e' ),
		WidgetMenuItem.Submenu( "view", "View", [
			WidgetMenuItem.Command( "view.go-line", "Go to Line:Column...", GoToLine, 'l',
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'g' ) ),
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
		"dedit-about", "About dedit",
		$"dedit {typeof( DeditMenu ).Assembly.GetName().Version?.ToString( 3 ) ?? "0.1.0"}\n" +
		"Copyright (C) 2026 Timothy J. Bruce\nGPL-3.0-or-later" );

	internal static WidgetMessageDialog CreateDirtyCloseDialog( string id, string caption, WidgetStyles styles ) => new WidgetMessageDialog(
		id,
		$"Unsaved Changes — {DocumentManager.SanitizeDisplay( caption )}",
		"Do you want to save these changes?",
		[
			new( "yes", "Yes", WidgetDialogOutcome.Yes ) { Mnemonic = 'Y' },
			new( "no", "Don't Save", WidgetDialogOutcome.No ) { Mnemonic = 'N' },
			new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) {
				Mnemonic = 'C', IsDefault = true, IsCancel = true
			}
		]
	) { Styles = styles ?? throw new ArgumentNullException( nameof( styles ) ) };

	internal static WidgetMessageDialog CreateExternalChangeDialog( string id, string caption, bool hasUnsavedChanges ) => new(
		id, "External file changed",
		$"Reload {Icod.Dedit.Documents.DocumentManager.SanitizeDisplay( caption )} from disk? " +
		( hasUnsavedChanges ? "Yes discards unsaved edits. " : "" ) +
		"No keeps the editor copy; Save As can preserve it separately.",
		[
			new( "yes", "Yes", WidgetDialogOutcome.Yes ) { Mnemonic = 'Y' },
			new( "no", "No", WidgetDialogOutcome.No ) { Mnemonic = 'N', IsDefault = true }
		] );

	internal static WidgetMessageDialog CreateOverwriteDialog( string id, string caption ) => new(
		id, "Overwrite file?", $"Replace {DocumentManager.SanitizeDisplay( caption )}?",
		[
			new( "yes", "Yes", WidgetDialogOutcome.Yes ) { Mnemonic = 'Y' },
			new( "no", "No", WidgetDialogOutcome.No ) { Mnemonic = 'N', IsDefault = true }
		] );

	internal static void UpdateState( WidgetMenuBar menu, WidgetTextEditor? editor, bool canSave = false, WidgetSearchOptions? searchOptions = null, TextFileEncoding? encoding = null, int? indentationWidth = null ) {
		ArgumentNullException.ThrowIfNull( menu );
		bool undo = editor?.CanExecuteEditCommand( WidgetEditCommand.Undo ) == true;
		bool redo = editor?.CanExecuteEditCommand( WidgetEditCommand.Redo ) == true;
		bool cut = editor?.CanExecuteEditCommand( WidgetEditCommand.Cut ) == true;
		bool copy = editor?.CanExecuteEditCommand( WidgetEditCommand.Copy ) == true;
		bool paste = editor?.CanExecuteEditCommand( WidgetEditCommand.Paste ) == true;
		bool selectAll = editor?.CanExecuteEditCommand( WidgetEditCommand.SelectAll ) == true;
		menu.SetPresentation( [
			new WidgetMenuItemPresentation( "file", "File", true ),
			new WidgetMenuItemPresentation( "file.new", "New", true ),
			new WidgetMenuItemPresentation( "file.open", "Open...", true ),
			new WidgetMenuItemPresentation( "file.save", "Save", canSave || editor is not null ),
			new WidgetMenuItemPresentation( "file.save-as", "Save As...", editor is not null ),
			new WidgetMenuItemPresentation( "file.save-all", "Save All", editor is not null ),
			new WidgetMenuItemPresentation( "file.encoding", "Encoding", editor is not null ),
			new WidgetMenuItemPresentation( "file.indentation", "Indentation", editor is not null ),
			new WidgetMenuItemPresentation( "file.indentation.two", "2 Spaces", editor is not null, indentationWidth == 2 ),
			new WidgetMenuItemPresentation( "file.indentation.four", "4 Spaces", editor is not null, indentationWidth == 4 ),
			new WidgetMenuItemPresentation( "file.indentation.eight", "8 Spaces", editor is not null, indentationWidth == 8 ),
			new WidgetMenuItemPresentation( "file.line-endings", "Line Endings", editor is not null ),
			new WidgetMenuItemPresentation( "file.line-endings.lf", "Unix LF", editor is not null ),
			new WidgetMenuItemPresentation( "file.line-endings.crlf", "Windows CRLF", editor is not null ),
			new WidgetMenuItemPresentation( "file.line-endings.cr", "Classic Mac CR", editor is not null ),
			new WidgetMenuItemPresentation( "file.encoding.utf8", "UTF-8", editor is not null, encoding == TextFileEncoding.Utf8 ),
			new WidgetMenuItemPresentation( "file.encoding.utf8-bom", "UTF-8 BOM", editor is not null, encoding == TextFileEncoding.Utf8Bom ),
			new WidgetMenuItemPresentation( "file.encoding.utf16-le", "UTF-16 LE", editor is not null, encoding == TextFileEncoding.Utf16LittleEndian ),
			new WidgetMenuItemPresentation( "file.encoding.utf16-be", "UTF-16 BE", editor is not null, encoding == TextFileEncoding.Utf16BigEndian ),
			new WidgetMenuItemPresentation( "file.close", "Close", editor is not null ),
			new WidgetMenuItemPresentation( "file.close-all", "Close All", editor is not null ),
			new WidgetMenuItemPresentation( "file.exit", "Exit", true ),
			new WidgetMenuItemPresentation( "edit", "Edit", editor is not null || undo || redo || cut || copy || paste || selectAll ),
			new WidgetMenuItemPresentation( "edit.undo", "Undo", undo ),
			new WidgetMenuItemPresentation( "edit.redo", "Redo", redo ),
			new WidgetMenuItemPresentation( "edit.cut", "Cut", cut ),
			new WidgetMenuItemPresentation( "edit.copy", "Copy", copy ),
			new WidgetMenuItemPresentation( "edit.paste", "Paste", paste ),
			new WidgetMenuItemPresentation( "edit.select-all", "Select All", selectAll ),
			new WidgetMenuItemPresentation( "edit.find", "Find...", editor is not null ),
			new WidgetMenuItemPresentation( "edit.replace", "Replace...", editor is not null ),
			new WidgetMenuItemPresentation( "edit.find-next", "Find Next", editor is not null ),
			new WidgetMenuItemPresentation( "edit.find-previous", "Find Previous", editor is not null ),
			new WidgetMenuItemPresentation( "edit.search-case", "Match Case", editor is not null, searchOptions?.MatchCase == true ),
			new WidgetMenuItemPresentation( "edit.search-whole-word", "Whole Word", editor is not null, searchOptions?.WholeWord == true ),
			new WidgetMenuItemPresentation( "edit.search-regex", "Regular Expression", editor is not null, searchOptions?.UseRegularExpression == true ),
			new WidgetMenuItemPresentation( "view", "View", editor is not null ),
			new WidgetMenuItemPresentation( "view.go-file", "Go to File...", editor is not null ),
			new WidgetMenuItemPresentation( "view.go-line", "Go to Line:Column...", editor is not null ),
			new WidgetMenuItemPresentation( "view.wrap", "Word Wrap", editor is not null,
				editor is not null && editor.View.WrapMode != CursesTextWrapMode.NoWrap )
		] );
	}

	internal static void ApplyWrap( WidgetTextEditor editor, bool enabled ) {
		ArgumentNullException.ThrowIfNull( editor );
		editor.View.WrapMode = enabled ? CursesTextWrapMode.Word : CursesTextWrapMode.NoWrap;
	}
}

internal sealed class DeditMenuPresenter {
	private readonly WidgetMenuBar menu;
	private DeditMenuPresentationState? state;

	internal DeditMenuPresenter( WidgetMenuBar menu ) =>
		this.menu = menu ?? throw new ArgumentNullException( nameof( menu ) );

	internal void Arrange( CursesRectangle bounds ) {
		if ( this.menu.Bounds != bounds ) this.menu.Arrange( bounds );
	}

	internal void Update( WidgetTextEditor? editor, bool canSave = false, WidgetSearchOptions? searchOptions = null,
		TextFileEncoding? encoding = null, int? indentationWidth = null ) {
		DeditMenuPresentationState current = new(
			editor is not null,
			canSave,
			editor?.CanExecuteEditCommand( WidgetEditCommand.Undo ) == true,
			editor?.CanExecuteEditCommand( WidgetEditCommand.Redo ) == true,
			editor?.CanExecuteEditCommand( WidgetEditCommand.Cut ) == true,
			editor?.CanExecuteEditCommand( WidgetEditCommand.Copy ) == true,
			editor?.CanExecuteEditCommand( WidgetEditCommand.Paste ) == true,
			editor?.CanExecuteEditCommand( WidgetEditCommand.SelectAll ) == true,
			searchOptions?.MatchCase == true,
			searchOptions?.WholeWord == true,
			searchOptions?.UseRegularExpression == true,
			encoding,
			indentationWidth,
			editor is not null && editor.View.WrapMode != CursesTextWrapMode.NoWrap );
		if ( current == this.state ) return;
		DeditMenu.UpdateState( this.menu, editor, canSave, searchOptions, encoding, indentationWidth );
		this.state = current;
	}

	private readonly record struct DeditMenuPresentationState(
		bool HasEditor,
		bool CanSave,
		bool CanUndo,
		bool CanRedo,
		bool CanCut,
		bool CanCopy,
		bool CanPaste,
		bool CanSelectAll,
		bool MatchCase,
		bool WholeWord,
		bool UseRegularExpression,
		TextFileEncoding? Encoding,
		int? IndentationWidth,
		bool Wrap );
}
