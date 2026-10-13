# Decision 0002: bytes, encoding, and line endings

Status: proposed for D1001 owner review.

New documents use strict UTF-8 without BOM, LF line endings and no automatic
final newline on every OS. This deliberately differs from Microsoft Edit's
platform-specific CRLF default. The default indentation is four spaces; tab
width is four. Per-document preferences may be changed without converting
existing bytes.

On open, recognize UTF-8 BOM, UTF-16 LE BOM and UTF-16 BE BOM. Otherwise decode
as strict UTF-8. Invalid byte sequences or a binary-looking input produce an
explicit diagnostic and do not replace any open document; no replacement
characters are silently saved. Legacy encodings and BOM-less UTF-16 require an
explicit later design rather than a heuristic guess. Bound file reads before
allocating the editor model; the limit must be measured and documented, not
advertised as unlimited.

The Widgets document retains exact text, including mixed CRLF/LF/CR, literal
tabs, and the presence or absence of a final terminator. Save encodes the
snapshot under the selected encoding/BOM without reformatting. A format
command may explicitly convert encoding/BOM or normalize all terminators;
show a preview/confirmation when conversion may be lossy. Reject unrepresentable
content and retain the original document. EOL conversion is a text edit and
must be undoable. An encoding/BOM change contributes to dirty state even if
the visible text is unchanged. Wrap and tab width are view preferences and
do not dirty the file by themselves.

The saved checkpoint identifies document text and save-affecting metadata.
Undo to that checkpoint clears dirty; redo restores it. Save writes an immutable
snapshot and marks only that snapshot saved; edits made during the write stay
dirty. A failed write leaves the checkpoint, buffer and original file intact.
Capture a byte fingerprint at load and recheck it before a replacement, even
when file size and modification time match. The check is not a portable
cross-process atomic compare-and-swap; surface conflicts and document the
remaining race rather than claiming a guarantee the filesystem cannot supply.

Acceptance: byte-for-byte unedited round trips of BOM variants, mixed EOLs,
empty files and final newline; invalid-byte rejection; explicit conversion;
undo/redo to checkpoint; save race and injected failure tests in D1003/D1005/D1006.
