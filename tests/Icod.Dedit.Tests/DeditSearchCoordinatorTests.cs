/*
	Icod.Dedit.Tests
	Checks search form requests against the active tab and revision.
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
using Icod.Dedit.Documents;
using Icod.Dedit.Interaction;
using Icod.Widgets;
using Xunit;

public sealed class DeditSearchCoordinatorTests {
	[Fact]
	public void FindRequestSelectsMatchAndWrapsWithinTheSameTab() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "one two one" ), TimeSpan.Zero );
		DeditSearchCoordinator search = new( host, manager );
		WidgetSearchForm form = search.Open( entry, allowReplace: false );
		host.Focus( form.QueryInput );
		host.Process( WidgetInput.Paste( "one" ), TimeSpan.Zero );
		WidgetSearchRequestedAction first = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		Assert.True( search.CompleteRequest( first, TimeSpan.FromMilliseconds( 1 ), CancellationToken.None ).Found );
		Assert.Equal( 0, entry.Shell.Editor.View.SelectionStart );
		Assert.Equal( 3, entry.Shell.Editor.View.SelectionEnd );
		WidgetSearchRequestedAction next = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.True( search.CompleteRequest( next, TimeSpan.FromMilliseconds( 2 ), CancellationToken.None ).Found );
		Assert.Equal( 8, entry.Shell.Editor.View.SelectionStart );
		WidgetSearchRequestedAction wrapped = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.True( search.CompleteRequest( wrapped, TimeSpan.FromMilliseconds( 3 ), CancellationToken.None ).Found );
		Assert.Equal( 0, entry.Shell.Editor.View.SelectionStart );
	}

	[Fact]
	public void ReplaceAllFromFormIsUndoableAndRejectsStaleRequest() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "one one" ), TimeSpan.Zero );
		DeditSearchCoordinator search = new( host, manager );
		WidgetSearchForm form = search.Open( entry, allowReplace: true );
		host.Focus( form.QueryInput ); host.Process( WidgetInput.Paste( "one" ), TimeSpan.Zero );
		host.Focus( form.ReplacementInput! ); host.Process( WidgetInput.Paste( "two" ), TimeSpan.Zero );
		WidgetSearchRequest request = form.Submit( WidgetSearchIntent.ReplaceAll );
		WidgetSearchRequestedAction action = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single( host.DrainActions() ) );
		Assert.Equal( request, action.Request );
		Assert.True( search.CompleteRequest( action, TimeSpan.FromMilliseconds( 1 ), CancellationToken.None ).Found );
		Assert.Equal( "two two", entry.Shell.Document.Read( 0, 7 ) );
		entry.Shell.Document.Undo();
		Assert.Equal( "one one", entry.Shell.Document.Read( 0, 7 ) );
		Assert.NotNull( search.CompleteRequest( action, TimeSpan.FromMilliseconds( 2 ), CancellationToken.None ).Error );
		Assert.Equal( "one one", entry.Shell.Document.Read( 0, 7 ) );
	}

	[Fact]
	public void RepeatAfterFormClosesMovesForwardAndBackwardOnCurrentRevision() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		entry.Shell.Process( WidgetInput.Paste( "one two one" ), TimeSpan.Zero );
		DeditSearchCoordinator search = new( host, manager );
		WidgetSearchForm form = search.Open( entry, allowReplace: false );
		host.Focus( form.QueryInput ); host.Process( WidgetInput.Paste( "one" ), TimeSpan.Zero );
		WidgetSearchRequestedAction request = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		Assert.True( search.CompleteRequest( request, TimeSpan.FromMilliseconds( 1 ), CancellationToken.None ).Found );
		WidgetDialogCompletedAction closed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.True( search.Close( closed ) );
		Assert.Equal( new SearchHit( 8, 3 ), search.Repeat( entry, previous: false, CancellationToken.None ) );
		Assert.Equal( new SearchHit( 0, 3 ), search.Repeat( entry, previous: true, CancellationToken.None ) );
		entry.Shell.Process( WidgetInput.Paste( "!" ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.NotNull( search.Repeat( entry, previous: false, CancellationToken.None ) );
		DocumentEntry other = manager.OpenUntitled();
		Assert.Null( search.Repeat( other, previous: false, CancellationToken.None ) );
	}
	[Fact]
	public void SearchOptionsChosenBeforeOpeningAreAppliedToWidgetsForm() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default );
		using DocumentManager manager = new( host );
		DocumentEntry entry = manager.OpenUntitled();
		DeditSearchCoordinator search = new( host, manager );
		WidgetSearchOptions options = new( MatchCase: true, WholeWord: true, UseRegularExpression: true );
		search.SetOptions( options );
		WidgetSearchForm form = search.Open( entry, allowReplace: false );
		Assert.Equal( options, form.Options );
	}
}
