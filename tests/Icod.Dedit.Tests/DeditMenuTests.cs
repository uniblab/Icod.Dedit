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
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class DeditMenuTests {
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
}
