/*
	Icod.Dedit
	Coordinates the consumer-sized About dialog.
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

using Icod.Widgets;

internal sealed class DeditAboutCoordinator {
	private const int DialogColumns = 45;
	private const int DialogRows = 7;
	private readonly WidgetHost host;
	private WidgetMessageDialog? dialog;

	internal DeditAboutCoordinator( WidgetHost host ) =>
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );

	internal void Open() {
		WidgetMessageDialog next = DeditMenu.CreateAboutDialog();
		this.dialog = next;
		this.host.ShowModal( next );
		this.Arrange();
	}

	internal void Arrange() {
		if ( this.dialog is not { IsOpen: true } current ) return;
		DeditDialogLayout.ArrangeCentered( current, this.host.Screen.Bounds, DialogRows, DialogColumns );
	}
}
