/*
	Icod.Dedit
	Presents the document tab strip and status line for active and empty workspaces.
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

internal sealed class DeditWorkspacePresenter {
	private readonly WidgetTabStrip tabs;

	internal DeditWorkspacePresenter( WidgetTabStrip tabs ) =>
		this.tabs = tabs ?? throw new ArgumentNullException( nameof( tabs ) );

	internal void Update( IReadOnlyList<WidgetTab> documents, string? activeKey, CursesRectangle screenBounds ) {
		ArgumentNullException.ThrowIfNull( documents );
		this.tabs.SetTabs( documents, activeKey );
		this.tabs.IsVisible = activeKey is not null;
		if ( this.tabs.IsVisible ) this.tabs.Arrange( new CursesRectangle(
			Math.Min( 1, screenBounds.Rows ), screenBounds.Column,
			Math.Min( 1, Math.Max( 0, screenBounds.Rows - 1 ) ), screenBounds.Columns ) );
	}

	internal static string StatusText( DocumentEntry? active, string? saveError, IReadOnlyList<string> openErrors ) {
		ArgumentNullException.ThrowIfNull( openErrors );
		if ( saveError is not null ) return saveError;
		if ( openErrors.Count > 0 ) return $"Open error ({openErrors.Count}): {openErrors[ 0 ]}";
		return active is null ? "" : EditorStatus.Format( active );
	}
}
