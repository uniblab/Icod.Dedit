/*
	Icod.Dedit.Tests
	Checks document workspace chrome for active and empty states.
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
using Icod.Dedit.Presentation;
using Icod.Widgets;
using Xunit;

public sealed class DeditWorkspacePresenterTests {
	[Fact]
	public void EmptyWorkspaceHidesTabsClearsFocusAndNewDocumentRestoresThem() {
		using WidgetHost host = new( new CursesScreen( 50, 15 ), WidgetTheme.Default );
		WidgetTabStrip tabs = new( "tabs" ); host.Attach( tabs );
		DeditWorkspacePresenter presenter = new( tabs );
		using DocumentManager documents = new( host );

		tabs.Arrange( new( 1, 0, 1, 50 ) );
		Assert.True( host.Focus( tabs ) );
		presenter.Update( documents.Tabs, documents.Active?.Key, host.Screen.Bounds );

		Assert.False( tabs.IsVisible );
		Assert.Null( host.FocusedWidget );
		Assert.False( host.Focus( tabs ) );
		Assert.Equal( "", DeditWorkspacePresenter.StatusText( documents.Active, null, [] ) );

		DocumentEntry opened = documents.OpenUntitled();
		presenter.Update( documents.Tabs, documents.Active?.Key, host.Screen.Bounds );

		Assert.True( tabs.IsVisible );
		Assert.Equal( new CursesRectangle( 1, 0, 1, 50 ), tabs.Bounds );
		Assert.True( host.Focus( tabs ) );
		Assert.StartsWith( "Untitled |", DeditWorkspacePresenter.StatusText( opened, null, [] ) );
	}

	[Fact]
	public void EmptyWorkspaceStillReportsSaveAndOpenErrors() {
		Assert.Equal( "Save failed.", DeditWorkspacePresenter.StatusText( null, "Save failed.", [ "Open failed." ] ) );
		Assert.Equal( "Open error (2): First", DeditWorkspacePresenter.StatusText( null, null, [ "First", "Second" ] ) );
	}
}
