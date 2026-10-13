/*
	Icod.Dedit
	Owns the terminal editor's event loop and session lifetime.
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

namespace Icod.Dedit.Application;

using Icod.Dedit.CommandLine;
using Icod.Widgets;

internal enum DeditEventKind { Input, Resize, Stop, Timeout }
internal readonly record struct DeditEvent( DeditEventKind Kind, WidgetInput? Input = null );

internal interface IDeditTerminal : IAsyncDisposable {
	ValueTask<DeditEvent> ReadAsync( CancellationToken cancellationToken );
	ValueTask DrawAsync( IReadOnlyList<string> captions, TimeSpan elapsed, CancellationToken cancellationToken );
	void Process( WidgetInput input, TimeSpan elapsed );
	void Resize();
}

internal static class DeditApplication {
	internal static async Task RunAsync( DeditOptions options, Func<ValueTask<IDeditTerminal>> open,
		Func<TimeSpan> clock, CancellationToken cancellationToken = default ) {
		ArgumentNullException.ThrowIfNull( options ); ArgumentNullException.ThrowIfNull( open ); ArgumentNullException.ThrowIfNull( clock );
		if ( options.Mode != DeditOptionMode.Edit ) throw new ArgumentException( "Only edit mode can open the terminal.", nameof( options ) );
		await using IDeditTerminal terminal = await open();
		bool redraw = true;
		while ( true ) {
			cancellationToken.ThrowIfCancellationRequested();
			if ( redraw ) { await terminal.DrawAsync( options.Captions, clock(), cancellationToken ); redraw = false; }
			DeditEvent current = await terminal.ReadAsync( cancellationToken );
			switch ( current.Kind ) {
				case DeditEventKind.Stop: return;
				case DeditEventKind.Resize: terminal.Resize(); redraw = true; break;
				case DeditEventKind.Input when current.Input is { } input:
					if ( input.Kind == WidgetInputKind.EndOfInput || IsQuit( input ) ) return;
					terminal.Process( input, clock() ); redraw = true; break;
			}
		}
	}

	private static bool IsQuit( WidgetInput input ) => input.Kind == WidgetInputKind.Key
		&& input.KeyPhase is null or Icod.DCurses.CursesKeyEventPhase.Press
		&& input.Modifiers.HasFlag( WidgetInputModifiers.Control )
		&& input.Character?.Value is 'q' or 'Q';
}
