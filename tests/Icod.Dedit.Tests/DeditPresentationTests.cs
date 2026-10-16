/*
	Icod.Dedit.Tests
	Checks application-owned theme and physical-caret presentation.
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

public sealed class DeditPresentationTests {
	[Fact]
	public void ColorThemeDistinguishesSelectionWithoutReverseVideo() {
		CursesPresentationCapabilities capabilities = Capabilities( 256, true, true, true,
			CursesTextAttributes.Reverse | CursesTextAttributes.Bold | CursesTextAttributes.Underline );
		WidgetTheme theme = DeditTheme.Create( capabilities );
		CursesStyle normal = theme.Resolve( WidgetStyleRole.EditorText );
		CursesStyle selection = theme.Resolve( WidgetStyleRole.TextSelection );

		Assert.NotEqual( normal.Background, selection.Background );
		Assert.Equal( CursesTextAttributes.None,
			selection.Attributes & ( CursesTextAttributes.Reverse | CursesTextAttributes.Standout ) );
	}

	[Fact]
	public async Task CaretCoordinatorShowsPositionAndStyleThenHidesAndReleases() {
		FakeCaretSession session = new();
		await using DeditCaretCoordinator coordinator = new( session );
		await coordinator.ApplyAsync( new WidgetCaretIntent( true, 4, 7, CursesCursorStyle.SteadyBar, 1 ), CancellationToken.None );

		Assert.Equal( ( 4, 7 ), session.Position ); Assert.True( session.Visible );
		Assert.Equal( [ CursesCursorStyle.SteadyBar ], session.AcquiredStyles );
		FakeLease lease = Assert.Single( session.Leases ); Assert.False( lease.Disposed );

		await coordinator.ApplyAsync( null, CancellationToken.None );
		Assert.False( session.Visible ); Assert.True( lease.Disposed );
	}

	private static CursesPresentationCapabilities Capabilities( int colors, bool rgb, bool foreground, bool background,
		CursesTextAttributes attributes ) {
		var constructor = typeof( CursesPresentationCapabilities ).GetConstructors(
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )
			.Single( candidate => candidate.GetParameters().Length == 11 );
		return (CursesPresentationCapabilities)constructor.Invoke( [ colors, rgb, foreground, background, true, attributes,
			CursesTextAttributes.None, false, true, true, false ] );
	}

	private sealed class FakeCaretSession : IDeditCaretSession {
		internal ( int Row, int Column ) Position { get; private set; }
		internal bool Visible { get; private set; }
		internal List<CursesCursorStyle> AcquiredStyles { get; } = [];
		internal List<FakeLease> Leases { get; } = [];
		public ValueTask<IAsyncDisposable?> AcquireStyleAsync( CursesCursorStyle style, CancellationToken cancellationToken ) {
			AcquiredStyles.Add( style ); FakeLease lease = new(); Leases.Add( lease ); return ValueTask.FromResult<IAsyncDisposable?>( lease );
		}
		public ValueTask SetPositionAsync( int row, int column, CancellationToken cancellationToken ) {
			Position = ( row, column ); return ValueTask.CompletedTask;
		}
		public ValueTask SetVisibilityAsync( bool visible, CancellationToken cancellationToken ) {
			Visible = visible; return ValueTask.CompletedTask;
		}
	}

	private sealed class FakeLease : IAsyncDisposable {
		internal bool Disposed { get; private set; }
		public ValueTask DisposeAsync() { this.Disposed = true; return ValueTask.CompletedTask; }
	}
}
