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
	internal static WidgetStyles CreateDirtyDialogStyles( CursesPresentationCapabilities capabilities ) {
		CursesColor red = capabilities.SupportsDirectRgb ? CursesColor.Rgb( 192, 0, 0 )
			: capabilities.IndexedColorCount > 7 ? CursesColor.Indexed( 1 ) : CursesColor.Default;
		CursesColor white = capabilities.SupportsDirectRgb ? CursesColor.Rgb( 255, 255, 255 )
			: capabilities.IndexedColorCount > 7 ? CursesColor.Indexed( 7 ) : CursesColor.Default;
		CursesTextAttributes emphasis = capabilities.SupportsBold ? CursesTextAttributes.Bold
			: capabilities.SupportsUnderline ? CursesTextAttributes.Underline
			: capabilities.SupportsStandout ? CursesTextAttributes.Standout : CursesTextAttributes.None;
		CursesTextAttributes focused = red.IsDefault && white.IsDefault
			? capabilities.SupportsReverse ? CursesTextAttributes.Reverse
			: capabilities.SupportsStandout ? CursesTextAttributes.Standout : emphasis
			: CursesTextAttributes.None;
		CursesTextAttributes disabled = capabilities.SupportsDim ? CursesTextAttributes.Dim : CursesTextAttributes.None;

		CursesStyle Style( CursesColor foreground, CursesColor background, CursesTextAttributes attributes = CursesTextAttributes.None ) {
			CursesColor front = capabilities.SupportsForegroundColor ? foreground : CursesColor.Default;
			CursesColor back = capabilities.SupportsBackgroundColor ? background : CursesColor.Default;
			CursesTextAttributes allowed = capabilities.SupportedAttributes;
			if ( !front.IsDefault || !back.IsDefault ) allowed &= ~capabilities.ColorRestrictedAttributes;
			return new CursesStyle( front, back, attributes & allowed );
		}

		CursesStyle ordinary = Style( white, red, red.IsDefault && white.IsDefault ? emphasis : CursesTextAttributes.None );
		CursesStyle emphasized = Style( white, red, emphasis );
		return WidgetStyles.Empty
			.With( WidgetStyleRole.DialogSurface, ordinary )
			.With( WidgetStyleRole.Text, ordinary )
			.With( WidgetStyleRole.DialogTitle, emphasized )
			.With( WidgetStyleRole.DialogBorder, emphasized )
			.With( WidgetStyleRole.DialogAction, ordinary )
			.With( WidgetStyleRole.DialogDefaultAction, emphasized )
			.With( WidgetStyleRole.DialogFocusedAction, Style( red, white, focused ) )
			.With( WidgetStyleRole.DialogDisabledAction, Style( white, red, disabled ) );
	}

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
