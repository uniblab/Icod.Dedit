/*
	Icod.Dedit
	Defines application presentation defaults from terminal capabilities.
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

internal static class DeditTheme {
	internal static WidgetTheme Create( CursesPresentationCapabilities capabilities ) {
		WidgetTheme theme = WidgetPalette.Create( capabilities );
		if ( !capabilities.SupportsForegroundColor || !capabilities.SupportsBackgroundColor ) return theme;
		CursesStyle normal = theme.Resolve( WidgetStyleRole.EditorText );
		CursesStyle selection = theme.Resolve( WidgetStyleRole.TextSelection );
		CursesStyle accent = theme.Resolve( WidgetStyleRole.Border );
		if ( normal.Background.IsDefault || accent.Foreground.IsDefault || normal.Background == accent.Foreground ) return theme;
		selection = new CursesStyle( normal.Background, accent.Foreground,
			selection.Attributes & ~( CursesTextAttributes.Reverse | CursesTextAttributes.Standout ) );
		return theme with { Parts = theme.Parts.With( WidgetStyleRole.TextSelection, selection ) };
	}
}
