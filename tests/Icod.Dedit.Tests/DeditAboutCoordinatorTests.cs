/*
	Icod.Dedit.Tests
	Checks Dedit-owned About dialog presentation.
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
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class DeditAboutCoordinatorTests {
	[Theory]
	[InlineData( 100, 30, 11, 27, 7, 45 )]
	[InlineData( 20, 3, 0, 0, 3, 20 )]
	public void OpenUsesDeditSpecifiedCompactBounds( int columns, int rows,
		int expectedRow, int expectedColumn, int expectedRows, int expectedColumns ) {
		using WidgetHost host = new( new CursesScreen( columns, rows ), WidgetTheme.Default );
		DeditAboutCoordinator about = new( host );

		about.Open();

		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );
		Assert.Equal( new CursesRectangle( expectedRow, expectedColumn, expectedRows, expectedColumns ), dialog.Bounds );
	}

	[Fact]
	public void OpenDialogRecentersAndClampsAfterResize() {
		CursesScreen screen = new( 100, 30 );
		using WidgetHost host = new( screen, WidgetTheme.Default );
		DeditAboutCoordinator about = new( host );
		about.Open();
		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );

		screen.Resize( 60, 10 );
		about.Arrange();
		Assert.Equal( new CursesRectangle( 1, 7, 7, 45 ), dialog.Bounds );

		screen.Resize( 30, 4 );
		about.Arrange();
		Assert.Equal( new CursesRectangle( 0, 0, 4, 30 ), dialog.Bounds );
	}

	[Fact]
	public void UnchangedLayoutBetweenPointerPressAndReleasePreservesOkClick() {
		using WidgetHost host = new( new CursesScreen( 50, 14 ), WidgetTheme.Default );
		DeditAboutCoordinator about = new( host );
		about.Open();
		WidgetMessageDialog dialog = Assert.IsType<WidgetMessageDialog>( host.FocusedWidget );
		host.Render();
		int row = dialog.Bounds.BottomExclusive - 2;
		int column = dialog.Bounds.Column + 1;

		Assert.Empty( host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press,
			CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero ).Actions );
		about.Arrange();
		host.Render();
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Release,
				CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Ok, completed.Result.Outcome );
	}
}
