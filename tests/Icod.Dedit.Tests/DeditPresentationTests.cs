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
	private static readonly WidgetStyleRole[] DirtyDialogRoles = [
		WidgetStyleRole.DialogSurface,
		WidgetStyleRole.Text,
		WidgetStyleRole.DialogTitle,
		WidgetStyleRole.DialogBorder,
		WidgetStyleRole.DialogAction,
		WidgetStyleRole.DialogDefaultAction,
		WidgetStyleRole.DialogFocusedAction,
		WidgetStyleRole.DialogDisabledAction
	];

	[Fact]
	public void RgbDirtyDialogUsesWhiteOnRedAndInvertedFocus() {
		WidgetStyles styles = DeditTheme.CreateDirtyDialogStyles( Capabilities( 256, true, true, true,
			CursesTextAttributes.Bold | CursesTextAttributes.Dim | CursesTextAttributes.Reverse ) );
		CursesColor white = CursesColor.Rgb( 255, 255, 255 ); CursesColor red = CursesColor.Rgb( 192, 0, 0 );

		Assert.Equal( new CursesStyle( white, red ), Style( styles, WidgetStyleRole.DialogSurface ) );
		Assert.Equal( new CursesStyle( white, red ), Style( styles, WidgetStyleRole.Text ) );
		Assert.Equal( new CursesStyle( white, red ), Style( styles, WidgetStyleRole.DialogAction ) );
		Assert.Equal( new CursesStyle( red, white ),
			Style( styles, WidgetStyleRole.DialogFocusedAction ) );
	}

	[Fact]
	public void IndexedDirtyDialogUsesWhiteOnRedAndInvertedFocus() {
		WidgetStyles styles = DeditTheme.CreateDirtyDialogStyles( Capabilities( 8, false, true, true,
			CursesTextAttributes.Bold | CursesTextAttributes.Reverse ) );
		CursesColor white = CursesColor.Indexed( 7 ); CursesColor red = CursesColor.Indexed( 1 );

		Assert.Equal( new CursesStyle( white, red ), Style( styles, WidgetStyleRole.DialogSurface ) );
		Assert.Equal( new CursesStyle( red, white ),
			Style( styles, WidgetStyleRole.DialogFocusedAction ) );
	}

	[Fact]
	public void PartialColorDirtyDialogUsesOnlySupportedAxes() {
		WidgetStyles foregroundOnly = DeditTheme.CreateDirtyDialogStyles( Capabilities( 8, false, true, false,
			CursesTextAttributes.Bold | CursesTextAttributes.Reverse ) );
		WidgetStyles backgroundOnly = DeditTheme.CreateDirtyDialogStyles( Capabilities( 8, false, false, true,
			CursesTextAttributes.Bold | CursesTextAttributes.Reverse ) );

		Assert.Equal( CursesColor.Indexed( 7 ), Style( foregroundOnly, WidgetStyleRole.DialogSurface ).Foreground );
		Assert.True( Style( foregroundOnly, WidgetStyleRole.DialogSurface ).Background.IsDefault );
		Assert.True( Style( backgroundOnly, WidgetStyleRole.DialogSurface ).Foreground.IsDefault );
		Assert.Equal( CursesColor.Indexed( 1 ), Style( backgroundOnly, WidgetStyleRole.DialogSurface ).Background );
	}

	[Fact]
	public void MonochromeDirtyDialogUsesLegibleAttributeFallback() {
		WidgetStyles styles = DeditTheme.CreateDirtyDialogStyles( Capabilities( 0, false, false, false,
			CursesTextAttributes.Bold | CursesTextAttributes.Reverse | CursesTextAttributes.Dim ) );

		foreach ( WidgetStyleRole role in DirtyDialogRoles ) {
			CursesStyle style = Style( styles, role );
			Assert.True( style.Foreground.IsDefault ); Assert.True( style.Background.IsDefault );
			Assert.Equal( CursesTextAttributes.None, style.Attributes & ~(
				CursesTextAttributes.Bold | CursesTextAttributes.Reverse | CursesTextAttributes.Dim ) );
		}
		Assert.Equal( CursesTextAttributes.Bold, Style( styles, WidgetStyleRole.DialogSurface ).Attributes );
		Assert.Equal( CursesTextAttributes.Reverse, Style( styles, WidgetStyleRole.DialogFocusedAction ).Attributes );
	}

	[Fact]
	public void DirtyDialogStylesOverrideOnlyDialogPresentationRoles() {
		WidgetStyles styles = DeditTheme.CreateDirtyDialogStyles( Capabilities( 256, true, true, true,
			CursesTextAttributes.Bold | CursesTextAttributes.Dim | CursesTextAttributes.Reverse ) );

		foreach ( WidgetStyleRole role in Enum.GetValues<WidgetStyleRole>() )
			Assert.Equal( DirtyDialogRoles.Contains( role ), styles.TryGetValue( role, out _ ) );
	}

	[Fact]
	public void ColorThemeDistinguishesSelectionWithoutReverseVideo() {
		CursesPresentationCapabilities capabilities = Capabilities( 256, true, true, true,
			CursesTextAttributes.Reverse | CursesTextAttributes.Bold | CursesTextAttributes.Underline );
		WidgetTheme theme = DeditTheme.Create( capabilities );
		CursesStyle normal = theme.Resolve( WidgetStyleRole.EditorText );
		CursesStyle selection = theme.Resolve( WidgetStyleRole.TextSelection );
		CursesStyle gutter = theme.Resolve( WidgetStyleRole.EditorGutter );
		CursesStyle currentGutter = theme.Resolve( WidgetStyleRole.EditorGutterCurrentLine );

		Assert.NotEqual( normal.Background, selection.Background );
		Assert.Equal( normal.Background, selection.Foreground );
		Assert.Equal( CursesTextAttributes.None,
			selection.Attributes & ( CursesTextAttributes.Reverse | CursesTextAttributes.Standout ) );
		Assert.NotEqual( gutter, currentGutter );
		using WidgetHost host = new( new CursesScreen( 12, 5 ), theme );
		using EditorShell shell = new( host, "ab" ); shell.Editor.View.SetSelection( 0, 1 ); host.Render();
		Assert.Equal( currentGutter, host.Screen.StandardWindow.GetCell( 2, 0 ).Style );
		Assert.Equal( selection, host.Screen.StandardWindow.GetCell( 2, 2 ).Style );
		Assert.Equal( theme.Resolve( WidgetStyleRole.EditorCurrentLine ), host.Screen.StandardWindow.GetCell( 2, 3 ).Style );
	}

	[Fact]
	public void MonochromeThemeRetainsAttributeSelectionFallback() {
		CursesPresentationCapabilities capabilities = Capabilities( 0, false, false, false,
			CursesTextAttributes.Reverse );

		WidgetTheme theme = DeditTheme.Create( capabilities );
		Assert.Equal( CursesTextAttributes.Reverse, theme.Resolve( WidgetStyleRole.TextSelection ).Attributes );
		foreach ( WidgetStyleRole role in new[] { WidgetStyleRole.EditorGutter, WidgetStyleRole.EditorGutterCurrentLine } ) {
			CursesStyle style = theme.Resolve( role );
			Assert.Equal( CursesColorKind.Default, style.Foreground.Kind );
			Assert.Equal( CursesColorKind.Default, style.Background.Kind );
			Assert.Equal( CursesTextAttributes.None, style.Attributes & ~CursesTextAttributes.Reverse );
		}
	}

	[Fact]
	public void LimitedColorThemeRetainsAttributeSelectionFallback() {
		CursesPresentationCapabilities capabilities = Capabilities( 2, false, true, true,
			CursesTextAttributes.Reverse );

		Assert.Equal( CursesTextAttributes.Reverse,
			DeditTheme.Create( capabilities ).Resolve( WidgetStyleRole.TextSelection ).Attributes );
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

	private static CursesStyle Style( WidgetStyles styles, WidgetStyleRole role ) {
		Assert.True( styles.TryGetValue( role, out CursesStyle style ) ); return style;
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
