/*
	Icod.Dedit
	Applies explicit line-ending conversion as one undoable document edit.
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

using Icod.Dedit.Documents;
using Icod.Widgets;

internal static class DocumentFormatController {
	internal static WidgetTextEditStatus NormalizeLineEndings( DocumentEntry entry, string ending,
		TimeSpan timestamp, CancellationToken token = default ) {
		ArgumentNullException.ThrowIfNull( entry );
		if ( ending is not ( "\n" or "\r\n" or "\r" ) ) throw new ArgumentException( "Use LF, CRLF or CR.", nameof( ending ) );
		token.ThrowIfCancellationRequested();
		WidgetTextDocument document = entry.Shell.Document;
		using WidgetTextSnapshot snapshot = document.CreateSnapshot();
		string text = snapshot.Read( 0, checked( (int)snapshot.Length ) );
		List<WidgetTextEdit> edits = [];
		for ( int index = 0; index < text.Length; index++ ) {
			if ( ( index & 4095 ) == 0 ) token.ThrowIfCancellationRequested();
			char current = text[ index ];
			if ( current is not ( '\r' or '\n' ) ) continue;
			int length = current == '\r' && index + 1 < text.Length && text[ index + 1 ] == '\n' ? 2 : 1;
			if ( text.AsSpan( index, length ).SequenceEqual( ending ) == false )
				edits.Add( new WidgetTextEdit( index, length, ending ) );
			index += length - 1;
		}
		if ( edits.Count == 0 ) return WidgetTextEditStatus.NoChange;
		WidgetTextViewState view = entry.Shell.Editor.View;
		long anchor = Transform( view.Anchor, edits );
		long caret = Transform( view.Caret, edits );
		token.ThrowIfCancellationRequested();
		WidgetTextEditContext context = new( view.Id, view.Anchor, view.Caret, anchor, caret,
			WidgetTextEditKind.Programmatic, timestamp );
		return document.Apply( edits, snapshot.Revision, context ).Status;
	}

	private static long Transform( long position, IReadOnlyList<WidgetTextEdit> edits ) {
		long shift = 0;
		foreach ( WidgetTextEdit edit in edits ) {
			if ( position < edit.Start ) break;
			if ( position < edit.Start + edit.DeleteLength ) return edit.Start + shift;
			shift += edit.InsertText.Length - edit.DeleteLength;
		}
		return position + shift;
	}
}
