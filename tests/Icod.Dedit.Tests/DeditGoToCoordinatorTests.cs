/*
	Icod.Dedit.Tests
	Checks Go to Line form validation and revision binding.
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

using Icod.DCurses;
using Icod.Dedit.Documents;
using Icod.Dedit.Interaction;
using Icod.Widgets;
using Xunit;

public sealed class DeditGoToCoordinatorTests {
	[Fact]
	public void InvalidPositionStaysOpenAndAcceptedPositionMovesCaret() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default );
		using DocumentManager documents = new( host );
		DocumentEntry entry = documents.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "a\u0301\t😀\r\nnext" ), TimeSpan.Zero );
		DeditGoToCoordinator goTo = new( host, documents );
		WidgetFormDialog form = goTo.Open( entry );
		Assert.Same( goTo.Input, host.FocusedWidget );
		host.Process( WidgetInput.Paste( "8:1" ), TimeSpan.Zero );
		WidgetDialogValidationRequestedAction invalid = Assert.IsType<WidgetDialogValidationRequestedAction>(
			Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.True( goTo.Validate( invalid ) );
		Assert.True( form.IsOpen );
		Assert.NotNull( form.Error );
		goTo.Input.SetSelection( 0, goTo.Input.Value.Length );
		host.Process( WidgetInput.Paste( "2:3" ), TimeSpan.Zero );
		WidgetDialogValidationRequestedAction valid = Assert.IsType<WidgetDialogValidationRequestedAction>(
			Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.True( goTo.Validate( valid ) );
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) );
		Assert.True( goTo.Complete( completed ) );
		Assert.Equal( 9, entry.Shell.Editor.View.Caret );
	}
	[Fact]
	public void StaleOrCanceledDialogNeverMovesTheCaret() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default );
		using DocumentManager documents = new( host );
		DocumentEntry entry = documents.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "one\ntwo" ), TimeSpan.Zero );
		DeditGoToCoordinator goTo = new( host, documents );
		goTo.Open( entry );
		host.Process( WidgetInput.Paste( "2:2" ), TimeSpan.Zero );
		WidgetDialogValidationRequestedAction validation = Assert.IsType<WidgetDialogValidationRequestedAction>(
			Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		entry.Shell.Document.Undo();
		long originalCaret = entry.Shell.Editor.View.Caret;
		Assert.True( goTo.Validate( validation ) );
		Assert.Empty( host.DrainActions() );
		Assert.Equal( originalCaret, entry.Shell.Editor.View.Caret );
		WidgetDialogCompletedAction canceled = Assert.IsType<WidgetDialogCompletedAction>(
			Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
		Assert.True( goTo.Complete( canceled ) );
	}
}
