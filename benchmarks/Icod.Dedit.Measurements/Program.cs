/*
	Icod.Dedit.Measurements
	Measures document operations and viewport-local editor rendering.
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

namespace Icod.Dedit.Measurements;

using System.Diagnostics;
using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static void Main() {
		Console.WriteLine( "scenario,units,lines,load_ms,load_bytes,snapshot_bytes,edit_ms,edit_bytes,cold_render_ms,cold_render_bytes,warm_render_ms,warm_render_bytes" );
		Measure( "many-lines", string.Concat( Enumerable.Repeat( "0123456789\r\n", 100_000 ) ) );
		Measure( "single-line", new string( 'x', 1_000_000 ) );
		Measure( "unicode-mixed", string.Concat( Enumerable.Repeat( "a\u0301界👩‍💻\r\n", 20_000 ) ) );
	}

	private static void Measure( string name, string text ) {
		GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
		WidgetTextDocument? document = null;
		( double loadMs, long loadBytes ) = Timed( () => document = new WidgetTextDocument( [ text.AsMemory() ] ) );
		WidgetTextDocument current = document!;
		long lines = current.LineCount;
		long before = GC.GetAllocatedBytesForCurrentThread();
		using ( WidgetTextSnapshot snapshot = current.CreateSnapshot() ) { _ = snapshot.Revision; }
		long snapshotBytes = GC.GetAllocatedBytesForCurrentThread() - before;
		( double editMs, long editBytes ) = Timed( () => {
			WidgetTextEditResult result = current.Apply( [ new WidgetTextEdit( 0, 0, "X" ) ], current.Revision,
				new WidgetTextEditContext( Guid.Empty, 0, 0, 1, 1, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );
			if ( result.Status != WidgetTextEditStatus.Applied ) throw new InvalidOperationException( result.Status.ToString() );
		} );
		using WidgetTextViewState view = new( current );
		WidgetTextEditor editor = new( "editor", view, new WidgetEditorOptions { Indentation = "    ", MaximumProjectionSourceUnits = 4_096 } );
		using WidgetHost host = new( new CursesScreen( 80, 24 ), WidgetTheme.Default );
		host.Attach( editor ); editor.Arrange( new CursesRectangle( 2, 0, 20, 80 ) ); host.Focus( editor );
		( double coldMs, long coldBytes ) = Timed( host.Render );
		( double warmMs, long warmBytes ) = Timed( host.Render );
		Console.WriteLine( $"{name},{text.Length},{lines},{loadMs:F3},{loadBytes},{snapshotBytes},{editMs:F3},{editBytes},{coldMs:F3},{coldBytes},{warmMs:F3},{warmBytes}" );
	}

	private static ( double Milliseconds, long Bytes ) Timed( Action action ) {
		long before = GC.GetAllocatedBytesForCurrentThread(); long start = Stopwatch.GetTimestamp();
		action();
		return ( Stopwatch.GetElapsedTime( start ).TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before );
	}
}
