/*
	Icod.Dedit
	Applies Widgets search intent to one document revision.
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

namespace Icod.Dedit.Interaction;

using System.Text.RegularExpressions;
using Icod.Dedit.Documents;
using Icod.Widgets;

internal sealed class DeditSearchCoordinator {
	private readonly WidgetHost host;
	private readonly DocumentManager documents;
	private WidgetSearchForm? form;
	private DocumentEntry? entry;

	internal DeditSearchCoordinator( WidgetHost host, DocumentManager documents ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.documents = documents ?? throw new ArgumentNullException( nameof( documents ) );
	}

	internal WidgetSearchForm Open( DocumentEntry entry, bool allowReplace ) {
		ArgumentNullException.ThrowIfNull( entry );
		if ( this.documents.Find( entry.Key ) != entry ) throw new ArgumentException( "Document is not open.", nameof( entry ) );
		if ( this.form?.IsOpen == true ) throw new InvalidOperationException( "A search form is already open." );
		WidgetSearchForm next = new( "dedit-search-" + Guid.NewGuid().ToString( "N" ),
			allowReplace ? "Replace" : "Find", allowReplace );
		next.SetContext( entry.Shell.Document.Id, entry.Shell.Editor.View.Id, entry.Shell.Document.Revision );
		this.entry = entry;
		this.form = next;
		this.host.ShowModal( next );
		return next;
	}

	internal bool Matches( WidgetSearchRequestedAction action ) => this.form is { } active && action.WidgetId == active.Id;
	internal bool Close( WidgetDialogCompletedAction action ) {
		if ( this.form is null || action.WidgetId != this.form.Id ) return false;
		this.form = null; this.entry = null; return true;
	}

	internal WidgetSearchResult CompleteRequest( WidgetSearchRequestedAction action, TimeSpan elapsed, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( action );
		WidgetSearchRequest request = action.Request;
		if ( !this.Matches( action ) || this.entry is not { } target || this.documents.Find( target.Key ) != target )
			return new WidgetSearchResult( request, false, Error: "Search document is no longer open." );
		WidgetTextDocument document = target.Shell.Document;
		WidgetTextViewState view = target.Shell.Editor.View;
		if ( request.DocumentId != document.Id || request.ViewId != view.Id || request.Revision != document.Revision )
			return new WidgetSearchResult( request, false, Error: "Document changed; search again." );
		try {
			using WidgetTextSnapshot snapshot = document.CreateSnapshot();
			string text = snapshot.Read( 0, checked( (int)snapshot.Length ) );
			SearchQuery query = new( request.Query, request.Options.UseRegularExpression,
				request.Options.MatchCase, request.Options.WholeWord );
			SearchResult matches = SearchController.Search( target.Key, snapshot.Revision, text, query, token );
			if ( matches.Hits.Count == 0 ) {
				WidgetSearchResult missing = new( request, false );
				this.form!.ApplyResult( missing ); return missing;
			}
			if ( request.Intent == WidgetSearchIntent.ReplaceAll ) {
				WidgetTextEditStatus status = SearchController.ReplaceAll( target, matches, request.Replacement ?? "", elapsed );
				if ( status != WidgetTextEditStatus.Applied ) return new WidgetSearchResult( request, false,
					Error: $"Replacement was not applied ({status})." );
				WidgetSearchResult applied = new( request, true );
				this.form!.ApplyResult( applied );
				this.form.SetContext( document.Id, view.Id, document.Revision );
				return applied;
			}
			SearchHit? hit = request.Intent == WidgetSearchIntent.ReplaceOne
				? matches.Hits.FirstOrDefault( item => view.SelectionStart == item.Start && view.SelectionEnd == item.Start + item.Length )
				: null;
			if ( hit is not null ) {
				SearchResult single = new( matches.DocumentKey, matches.Revision, matches.Query, [ hit ] );
				WidgetTextEditStatus status = SearchController.ReplaceAll( target, single, request.Replacement ?? "", elapsed );
				if ( status != WidgetTextEditStatus.Applied ) return new WidgetSearchResult( request, false,
					Error: $"Replacement was not applied ({status})." );
				WidgetSearchResult applied = new( request, true, hit.Start, hit.Length );
				this.form!.ApplyResult( applied );
				this.form.SetContext( document.Id, view.Id, document.Revision );
				return applied;
			}
			int offset = view.HasSelection
				? request.Options.Direction == WidgetSearchDirection.Forward ? checked( (int)view.SelectionEnd ) : checked( (int)view.SelectionStart )
				: checked( (int)view.Caret );
			hit = matches.Next( offset, request.Options.Direction == WidgetSearchDirection.Backward );
			view.SetSelection( hit!.Start, hit.Start + hit.Length );
			WidgetSearchResult found = new( request, true, hit.Start, hit.Length );
			this.form!.ApplyResult( found ); return found;
		} catch ( Exception exception ) when ( exception is ArgumentException or RegexMatchTimeoutException
			or TimeoutException or InvalidDataException ) {
			WidgetSearchResult failed = new( request, false, Error: exception.Message );
			this.form?.ApplyResult( failed ); return failed;
		}
	}
}
