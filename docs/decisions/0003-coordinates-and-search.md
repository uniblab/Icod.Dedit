# Decision 0003: positions, search, and interaction

Status: proposed for D1001 owner review.

The status and Go to Line use one-based logical line and grapheme column. A
literal tab and a wide emoji each count as one grapheme; terminal cells are
only a rendering coordinate. Reject zero, negative, missing, overflowing and
out-of-range line/column requests with an inline error; never silently clamp
to a different position. Internal Widgets offsets may be UTF-16 positions;
conversion to/from grapheme boundaries must be explicit. Up/Down keep a
preferred visual-cell column across short lines and wraps, then map back to a
valid grapheme boundary. Home/End target the logical line, not an individual
wrapped row. Shift extends the selection; bare movement collapses it.

Find and Replace use .NET regex syntax when the Regex option is selected.
Literal mode escapes the needle. Case-insensitive matching uses invariant
Unicode casing; whole-word means a match bounded by characters outside the
letter/number/mark/connector categories. Match and replacement are applied
only to the same document ID and revision searched. A regex has a finite
timeout and a cancellation token; all matches for Replace All are validated
before one undoable transaction begins. Empty matches advance by a grapheme
boundary (including the final boundary) so scanning terminates. Invalid or
timed-out patterns leave both the document and search form intact, with an
error message. Search wraps once; Next and Previous traverse in opposite
directions and report no match after one pass.

Editor Tab/Shift+Tab indent/outdent; F6/Shift+F6 moves among editor, tabs,
status and menu so the editor is not a keyboard trap. Form Tab traverses form
fields. Menus reserve unmodified mnemonics only while their level is open;
shortcuts are independent, modal scope blocks background commands, and Escape
always yields Cancel from a dialog. If a terminal cannot distinguish a chord,
the corresponding menu action remains available. Bracketed paste is text in
one undo transaction, not a sequence of keys. Key release events do not invoke
a command again. Pointer capture ends on release and all cancellation paths.

Acceptance: Unicode/tab/wide-cell navigation; invalid Go to positions;
case/whole-word/zero-width search; regex timeout; stale result rejection;
focus, modal, paste and pointer tests in D1003/D1004/D1007/D1008.
