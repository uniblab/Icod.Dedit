/*
	Icod.Dedit.Tests
	Tests for command-line and terminal-session ownership.
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

using System.Text;
using Icod.Dedit.Application;
using Icod.Dedit.CommandLine;
using Icod.Widgets;
using Xunit;

public sealed class DeditStartupTests {
	[Theory]
	[InlineData("--help")]
	[InlineData("-h")]
	[InlineData("--version")]
	[InlineData("-v")]
	public async Task InformationalArgumentsNeverOpenTerminal( string argument ) {
		StringWriter output = new(); StringWriter error = new();
		int code = await Program.RunAsync( [ argument ], output, error, () => throw new Exception( "Opened terminal" ) );
		Assert.Equal( 0, code ); Assert.NotEmpty( output.ToString() ); Assert.Equal( "", error.ToString() );
	}

	[Theory]
	[InlineData("--bad")]
	[InlineData("-z")]
	public async Task InvalidArgumentsDiagnoseWithoutOpeningTerminal( string argument ) {
		StringWriter error = new();
		int code = await Program.RunAsync( [ argument ], new StringWriter(), error, () => throw new Exception( "Opened terminal" ) );
		Assert.Equal( 2, code ); Assert.Contains( argument, error.ToString() );
	}

	[Fact]
	public void PathsRemainLiteralAndOrdered() {
		DeditOptions options = DeditOptions.Parse( [ "文書 一.txt", "a:b", "--", "-draft", "--help" ] );
		Assert.Equal( [ "文書 一.txt", "a:b", "-draft", "--help" ], options.Paths );
		Assert.Equal( DeditOptionMode.Edit, options.Mode );
		Assert.Equal( [ "Untitled" ], DeditOptions.Parse( [] ).Captions );
	}

	[Fact]
	public void UnsafePathCharactersAreEscapedOnlyInDisplayCaption() {
		DeditOptions options = DeditOptions.Parse( [ "a\nb\t\u001bc.txt" ] );
		Assert.Equal( "a\nb\t\u001bc.txt", options.Paths[ 0 ] );
		Assert.Equal( "a�b��c.txt", options.Captions[ 0 ] );
	}

	[Fact]
	public async Task NormalExitDisposesOneSession() {
		FakeTerminal terminal = new( [ new( DeditEventKind.Input, WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'q' ) ) ) ] );
		int opens = 0;
		await DeditApplication.RunAsync( DeditOptions.Parse( [ "first", "second" ] ), () => { opens++; return ValueTask.FromResult<IDeditTerminal>( terminal ); }, () => TimeSpan.Zero );
		Assert.Equal( 1, opens ); Assert.Equal( 1, terminal.DrawCount ); Assert.Equal( [ "first", "second" ], terminal.Captions ); Assert.True( terminal.Disposed );
	}

	[Fact]
	public async Task FailureAfterOpenStillDisposesSession() {
		FakeTerminal terminal = new( [] ) { ThrowOnDraw = true };
		await Assert.ThrowsAsync<IOException>( () => DeditApplication.RunAsync( DeditOptions.Parse( [] ), () => ValueTask.FromResult<IDeditTerminal>( terminal ), () => TimeSpan.Zero ) );
		Assert.True( terminal.Disposed );
	}

	[Fact]
	public async Task ResizeAndStopRepaintThenDispose() {
		FakeTerminal terminal = new( [ new( DeditEventKind.Resize ), new( DeditEventKind.Input, WidgetInput.Paste( "\u0011" ) ),
			new( DeditEventKind.Input, WidgetInput.Key( WidgetKey.Enter ) ), new( DeditEventKind.Stop ) ] );
		await DeditApplication.RunAsync( DeditOptions.Parse( [] ), () => ValueTask.FromResult<IDeditTerminal>( terminal ), () => TimeSpan.FromSeconds( 3 ) );
		Assert.True( terminal.Disposed ); Assert.Equal( 4, terminal.DrawCount ); Assert.Equal( 1, terminal.ResizeCount ); Assert.Equal( 2, terminal.ProcessCount );
	}

	[Fact]
	public async Task StartupFailureReturnsDiagnosticAndDisposes() {
		FakeTerminal terminal = new( [] ) { ThrowOnDraw = true };
		StringWriter error = new();
		int code = await Program.RunAsync( [], new StringWriter(), error, () => ValueTask.FromResult<IDeditTerminal>( terminal ) );
		Assert.Equal( 1, code ); Assert.Contains( "Simulated refresh failure", error.ToString() ); Assert.True( terminal.Disposed );
	}

	[Fact]
	public async Task WidgetCleanupFailureStillDisposesSession() {
		ThrowingDisposable host = new(); FakeAsyncDisposable session = new();
		await Assert.ThrowsAsync<IOException>( () => DeditTerminal.DisposeOwnedResourcesAsync( host, session ).AsTask() );
		Assert.True( session.Disposed );
	}

	[Fact]
	public async Task AggregateStartupFailureIsDiagnosed() {
		StringWriter error = new();
		int code = await Program.RunAsync( [], new StringWriter(), error,
			() => throw new AggregateException( "Terminal initialization and recovery failed.", new IOException( "initialization" ) ) );
		Assert.Equal( 1, code ); Assert.Contains( "Terminal initialization and recovery failed", error.ToString() );
	}

	private sealed class ThrowingDisposable : IDisposable { public void Dispose() => throw new IOException( "Widget disposal failed." ); }
	private sealed class FakeAsyncDisposable : IAsyncDisposable {
		public bool Disposed { get; private set; }
		public ValueTask DisposeAsync() { this.Disposed = true; return ValueTask.CompletedTask; }
	}

	private sealed class FakeTerminal( IEnumerable<DeditEvent> events ) : IDeditTerminal {
		private readonly Queue<DeditEvent> events = new( events );
		public bool Disposed { get; private set; }
		public bool ThrowOnDraw { get; init; }
		public int DrawCount { get; private set; }
		public int ResizeCount { get; private set; }
		public int ProcessCount { get; private set; }
		public string[] Captions { get; private set; } = [];
		public ValueTask<DeditEvent> ReadAsync( CancellationToken cancellationToken ) => ValueTask.FromResult( this.events.Dequeue() );
		public ValueTask DrawAsync( IReadOnlyList<string> captions, TimeSpan elapsed, CancellationToken cancellationToken ) {
			this.Captions = captions.ToArray(); this.DrawCount++;
			if ( this.ThrowOnDraw ) throw new IOException( "Simulated refresh failure" );
			return ValueTask.CompletedTask;
		}
		public void Process( WidgetInput input, TimeSpan elapsed ) => this.ProcessCount++;
		public void Resize() => this.ResizeCount++;
		public ValueTask DisposeAsync() { this.Disposed = true; return ValueTask.CompletedTask; }
	}
}
