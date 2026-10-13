/*
	Icod.Dedit.Tests
	Checks dirty-document quit confirmation before persistent saving exists.
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
using Icod.Dedit.Documents;
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class QuitConfirmationTests {
	[Fact]
	public void CleanWorkspaceExitsButDirtyWorkspaceNeedsExplicitYes() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using DocumentManager manager = new( host ); DocumentEntry first = manager.OpenUntitled();
		WidgetInput quit = CtrlQ();
		Assert.True( DeditTerminal.RequestQuit( host, menu, manager, quit, TimeSpan.FromMilliseconds( 1 ) ) );
		first.Shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.False( DeditTerminal.RequestQuit( host, menu, manager, quit, TimeSpan.FromMilliseconds( 3 ) ) );
		Assert.Equal( 1, host.ModalDepth );
		Assert.False( DeditTerminal.RequestQuit( host, menu, manager, quit, TimeSpan.FromMilliseconds( 4 ) ) );
		WidgetDialogCompletedAction canceled = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 5 ) ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, canceled.Result.Outcome );
		Assert.False( DeditTerminal.ConfirmedDiscard( canceled ) );
		Assert.Equal( "x", first.Shell.Document.Read( 0, 1 ) );
		Assert.False( DeditTerminal.RequestQuit( host, menu, manager, quit, TimeSpan.FromMilliseconds( 6 ) ) );
		host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 7 ) );
		WidgetDialogCompletedAction yes = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 8 ) ).Actions ) );
		Assert.True( DeditTerminal.ConfirmedDiscard( yes ) );
	}

	[Fact]
	public void InactiveDirtyDocumentAlsoBlocksQuit() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetMenuBar menu = DeditMenu.Create(); host.Attach( menu ); menu.Arrange( host.Screen.Bounds );
		using DocumentManager manager = new( host ); DocumentEntry first = manager.OpenUntitled();
		first.Shell.Process( WidgetInput.TextInput( new Rune( 'x' ) ), TimeSpan.FromMilliseconds( 1 ) );
		manager.OpenUntitled();
		Assert.False( DeditTerminal.RequestQuit( host, menu, manager, CtrlQ(), TimeSpan.FromMilliseconds( 2 ) ) );
		Assert.Equal( 1, host.ModalDepth );
	}

	private static WidgetInput CtrlQ() => WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'q' ) );
}
