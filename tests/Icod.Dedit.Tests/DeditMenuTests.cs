/*
	Icod.Dedit.Tests
	Integration tests for the editor command menu.
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

namespace Icod.Dedit.Tests;

using System.Text;
using Icod.DCurses;
using Icod.Dedit.Application;
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class DeditMenuTests {



	[Fact]
	public void FileCloseMenuAndShortcutInvokeSameCommand() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host, "draft" );
		DeditMenu.UpdateState( menu, shell.Editor, canSave: false );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'f' ) ), TimeSpan.FromMilliseconds( 1 ) );
		WidgetInvokedAction mnemonic = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'c' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		WidgetInvokedAction shortcut = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'w' ) ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( DeditMenu.Close, mnemonic.CommandId );
		Assert.Equal( mnemonic.CommandId, shortcut.CommandId );
	}

	[Fact]
	public void FileSaveAsMenuAndShortcutInvokeSameCommandForUntitled() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host, "draft" );
		DeditMenu.UpdateState( menu, shell.Editor, canSave: false );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'f' ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.True( menu.IsOpen );
		WidgetInvokedAction mnemonic = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'a' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		WidgetInvokedAction shortcut = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control | WidgetInputModifiers.Shift,
				new Rune( 's' ) ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( DeditMenu.SaveAs, mnemonic.CommandId );
		Assert.Equal( mnemonic.CommandId, shortcut.CommandId );
	}

	[Fact]
	public void FileSaveMenuAndShortcutInvokeTheSameCommand() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host, "text" );
		DeditMenu.UpdateState( menu, shell.Editor, canSave: true );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'f' ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.True( menu.IsOpen );
		WidgetInvokedAction mnemonic = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 's' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		WidgetInvokedAction shortcut = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 's' ) ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( DeditMenu.Save, mnemonic.CommandId );
		Assert.Equal( mnemonic.CommandId, shortcut.CommandId );
	}

	[Fact]
	public void ViewMnemonicAndDirectShortcutInvokeSameGoToFileCommand() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'v' ) ), TimeSpan.FromMilliseconds( 1 ) ).Actions );
		Assert.True( menu.IsOpen );
		WidgetInvokedAction mnemonic = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'f' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.False( menu.IsOpen );
		WidgetInvokedAction shortcut = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'p' ) ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( DeditMenu.GoToFile, mnemonic.CommandId );
		Assert.Equal( mnemonic.CommandId, shortcut.CommandId );
		Assert.False( menu.IsOpen );
	}

	[Fact]
	public void MenuEscapeClosesWithoutInvokingACommand() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'v' ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions );
		Assert.Equal( 1, menu.Depth );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 3 ) ).Actions );
		Assert.False( menu.IsOpen );
	}

	[Fact]
	public void ModalEscapeReportsCancelWithoutDispatchingBackgroundShortcut() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "about", "About dedit", "GPL-3.0-or-later" );
		host.ShowModal( dialog );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'p' ) ), TimeSpan.FromMilliseconds( 1 ) ).Actions );
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, completed.Result.Outcome );
	}

	[Fact]
	public void EditMenuUndoAndDirectRedoUseFocusedEditorHistory() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host );
		shell.Process( WidgetInput.Paste( "draft" ), TimeSpan.FromMilliseconds( 1 ) );
		DeditMenu.UpdateState( menu, shell.Editor );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'e' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.True( menu.IsOpen );
		Assert.Contains( shell.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'u' ) ), TimeSpan.FromMilliseconds( 3 ) ),
			action => action is WidgetTextEditedAction );
		Assert.Equal( "", shell.Document.Read( 0, 0 ) );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'y' ) ), TimeSpan.FromMilliseconds( 4 ) );
		Assert.Equal( "draft", shell.Document.Read( 0, 5 ) );
	}

	[Fact]
	public void UndoRemainsDisabledUntilThereIsHistory() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host ); DeditMenu.UpdateState( menu, shell.Editor );
		Assert.Empty( shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'e' ) ), TimeSpan.FromMilliseconds( 1 ) ) );
		Assert.True( menu.IsOpen );
		Assert.Empty( shell.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'u' ) ), TimeSpan.FromMilliseconds( 2 ) ) );
		Assert.Equal( 0, shell.Document.Length );
		shell.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) );
		shell.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) );
		shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromMilliseconds( 2 ) );
		DeditMenu.UpdateState( menu, shell.Editor );
		shell.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'e' ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.True( menu.IsOpen );
	}

	[Fact]
	public void WrapCommandChangesViewWithoutChangingDocumentText() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using EditorShell shell = new( host ); shell.Process( WidgetInput.Paste( "first line\nsecond line" ), TimeSpan.FromMilliseconds( 1 ) );
		DeditMenu.UpdateState( menu, shell.Editor );
		Assert.Equal( CursesTextWrapMode.NoWrap, shell.Editor.View.WrapMode );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'v' ) ), TimeSpan.FromMilliseconds( 2 ) );
		WidgetCheckedInvokedAction enabled = Assert.IsType<WidgetCheckedInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'w' ) ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( DeditMenu.Wrap, enabled.CommandId ); Assert.True( enabled.IsChecked );
		DeditMenu.ApplyWrap( shell.Editor, enabled.IsChecked );
		Assert.Equal( CursesTextWrapMode.Word, shell.Editor.View.WrapMode );
		DeditMenu.UpdateState( menu, shell.Editor );
		WidgetCheckedInvokedAction disabled = Assert.IsType<WidgetCheckedInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'z' ) ), TimeSpan.FromMilliseconds( 4 ) ).Actions ) );
		DeditMenu.ApplyWrap( shell.Editor, disabled.IsChecked );
		Assert.False( disabled.IsChecked ); Assert.Equal( CursesTextWrapMode.NoWrap, shell.Editor.View.WrapMode );
		Assert.Equal( "first line\nsecond line", shell.Document.Read( 0, checked( (int)shell.Document.Length ) ) );
		Assert.True( shell.Document.IsModified );
	}

	[Fact]
	public void AboutMenuOpensCaptionedOkDialogAndEscapeCancels() {
		using WidgetHost host = new( new CursesScreen( 50, 14 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'h' ) ), TimeSpan.FromMilliseconds( 1 ) );
		WidgetInvokedAction about = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Character, character: new Rune( 'a' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.Equal( DeditMenu.About, about.CommandId );
		WidgetMessageDialog dialog = DeditMenu.CreateAboutDialog(); host.ShowModal( dialog ); host.Render();
		Assert.Contains( "dedit", dialog.Title, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "GPL-3.0-or-later", dialog.Message );
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, completed.Result.Outcome );
	}

	[Fact]
	public void QuitKeyDoesNotBypassAnOpenModal() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		WidgetMessageDialog dialog = DeditMenu.CreateAboutDialog(); host.ShowModal( dialog );
		WidgetInput quit = WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'q' ) );
		Assert.False( DeditTerminal.CanQuitFromKey( host, menu, quit, TimeSpan.FromMilliseconds( 1 ) ) );
		WidgetDialogCompletedAction cancelled = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, cancelled.Result.Outcome );
		Assert.True( DeditTerminal.CanQuitFromKey( host, menu, quit, TimeSpan.FromMilliseconds( 3 ) ) );
	}

	[Fact]
	public void QuitKeyDoesNotBypassAnOpenMenu() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'v' ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.False( DeditTerminal.CanQuitFromKey( host, menu,
			WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'q' ) ), TimeSpan.FromMilliseconds( 2 ) ) );
		Assert.True( menu.IsOpen );
		host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 3 ) );
		host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 4 ) );
		Assert.True( DeditTerminal.CanQuitFromKey( host, menu,
			WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'q' ) ), TimeSpan.FromMilliseconds( 5 ) ) );
	}

	[Fact]
	public void F6CyclesFocusWithoutChangingEditorTextOrConsumingTabIndent() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); WidgetTabStrip tabs = new( "tabs" );
		host.Attach( menu ); host.Attach( tabs ); menu.Arrange( host.Screen.Bounds );
		tabs.SetTabs( [ new WidgetTab( "untitled", "Untitled" ) ], "untitled" );
		tabs.Arrange( new CursesRectangle( 1, 0, 1, 40 ) );
		using EditorShell shell = new( host ); Assert.Same( shell.Editor, host.FocusedWidget );
		Assert.True( DeditTerminal.TryCycleFocus( host, WidgetInput.FunctionKey( 6 ) ) );
		Assert.Same( menu, host.FocusedWidget );
		Assert.True( DeditTerminal.TryCycleFocus( host, WidgetInput.FunctionKey( 6, WidgetInputModifiers.Shift ) ) );
		Assert.Same( shell.Editor, host.FocusedWidget );
		Assert.False( DeditTerminal.TryCycleFocus( host, WidgetInput.FunctionKey( 6 ) with { KeyPhase = CursesKeyEventPhase.Release } ) );
		Assert.Same( shell.Editor, host.FocusedWidget );
		shell.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "    ", shell.Document.Read( 0, 4 ) );
		WidgetMessageDialog dialog = DeditMenu.CreateAboutDialog(); host.ShowModal( dialog );
		Assert.True( DeditTerminal.TryCycleFocus( host, WidgetInput.FunctionKey( 6 ) ) );
		Assert.Same( dialog, host.FocusedWidget );
	}
}
