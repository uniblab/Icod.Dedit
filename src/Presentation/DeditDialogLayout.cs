/*
	Icod.Dedit
	Applies consumer-owned dialog bounds.
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

internal static class DeditDialogLayout {
	internal static void ArrangeCentered( WidgetMessageDialog dialog, CursesRectangle viewport,
		int desiredRows, int desiredColumns ) {
		ArgumentNullException.ThrowIfNull( dialog );
		int rows = Math.Min( desiredRows, viewport.Rows );
		int columns = Math.Min( desiredColumns, viewport.Columns );
		CursesRectangle bounds = new(
			viewport.Row + ( viewport.Rows - rows ) / 2,
			viewport.Column + ( viewport.Columns - columns ) / 2,
			rows,
			columns );
		if ( dialog.Bounds != bounds ) dialog.Arrange( bounds );
	}
}
