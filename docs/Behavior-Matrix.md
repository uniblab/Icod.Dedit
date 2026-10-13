# dedit first-release behavior matrix

Status: D1001 accepted baseline. This records intended behavior and acceptance
tests, not a claim that later features are implemented.

The pinned reference is [Microsoft Edit v1.2.1](https://github.com/microsoft/edit/tree/v1.2.1),
especially its [menu definitions](https://github.com/microsoft/edit/blob/v1.2.1/src/bin/edit/draw_menubar.rs),
[application shortcuts](https://github.com/microsoft/edit/blob/v1.2.1/src/bin/edit/main.rs),
[text-buffer behavior](https://github.com/microsoft/edit/blob/v1.2.1/src/buffer/mod.rs),
and [command-line description](https://github.com/microsoft/edit/blob/v1.2.1/assets/manpage/edit.1).
“Adopt” means reproduce the user-visible behavior where the terminal reports the
gesture; “differ” is an intentional dedit rule; “defer” is outside the first
release. This is independent implementation, not Microsoft source reuse.

| Area | Reference and choice | dedit behavior | Owner / acceptance gate |
| --- | --- | --- | --- |
| Application shell | Differ: Edit has a menu and a document switcher, not persistent Notepad-style tabs. | Visible ordered tabs, active/modified state, duplicate-name disambiguation, overflow and close requests. Each tab owns an independent document/view; an empty Untitled replaces the final closed tab. | Dedit + Widgets / D1005 tab identity, switching, overflow and empty-workspace tests. |
| File menu | Adopt New, Open, Save, Save As, Close, Exit. Extend Save All and Close All. | Commands act on the active tab except explicitly all-document commands. Closing, quitting and Save All are cancellable. | Dedit / D1004 command routing; D1006 partial-save/cancel tests. |
| Edit menu | Adopt Undo, Redo, Cut, Copy, Paste, Find, Replace, Select All. | The focused field owns editing commands; disabled menu state follows the active target. Clipboard fallback remains usable if terminal transport is unavailable. | Widgets routing + Dedit / D1004 focus/modal/clipboard tests. |
| View menu | Adopt status access, Go to File, Go to Line and word-wrap toggle. | Go to File selects a tab; Go to Line accepts line:column. Wrap belongs to each view and never changes stored text. | Dedit + Widgets / D1004 and D1007 navigation tests. |
| Help menu | Adopt About. | Show dedit identity/version/license; no Microsoft branding. | Dedit / D1004 keyboard/dialog test. |
| Printing | Exclude. | No Print menu, shortcut, command or dependency. | Dedit / D1009 menu/package inspection. |
| Navigation | Adopt arrow, Home/End, Page Up/Down, Ctrl+Left/Right word movement, Ctrl+Home/End document movement; Shift extends selection. | Caret moves by Unicode text elements, never into a surrogate pair or grapheme; logical line and preferred visual-cell column survive vertical motion. Home/End use logical line boundaries, including when wrapped. | Widgets / D1003 Unicode, tabs, wide glyph and resize tests. |
| Selection and mouse | Adopt keyboard extension, drag selection and scroll wheel. Differ: explicitly capture drag outside the editor. | Double-click selects a word, triple-click a logical line; pointer capture ends on release, focus loss, modal entry, hide or disposal. Tiny terminals retain keyboard navigation. | Widgets/DCurses + Dedit / D1003-D1004 pointer and selection tests. |
| Editing | Adopt insert/overwrite, four-column tab stop, spaces by default, Tab indent and Shift+Tab outdent. | Typed Tab follows per-document indentation preference; selected lines indent/outdent as one undo transaction. Insert toggles overwrite. Pasted text is one edit and does not dispatch shortcuts. | Widgets + Dedit / D1003-D1004 edit/undo/paste tests. |
| Linewise clipboard | Adopt Edit's copy/cut of the current logical line when nothing is selected and linewise paste at line start. | Internal clipboard tracks the linewise flag. External clipboard is plain text, so a fresh external paste has no linewise flag. Preserve exact copied line terminator. | Widgets + Dedit / D1003-D1004 no-selection and round-trip tests. |
| Find and replace | Adopt literal/regex, case/whole-word options, Find Next, Replace, Replace All. Extend Find Previous. | Explicit regex timeout/cancellation; results carry document ID and revision. Zero-length matches advance safely; invalid patterns keep the form open. | Dedit search execution + Widgets forms / D1007 tests. |
| Status | Adopt line/column, newline, encoding, indentation and overwrite indicators. | State is per document/view. Encoding/EOL conversions are explicit and undoable or checkpoint-aware; opening never silently normalizes mixed EOLs. | Dedit + Widgets status / D1005-D1007 format/dirty tests. |
| File input | Adopt one or more positional paths, `-h`/`--help` and `-v`/`--version`. Differ: initially no `FILE:LINE:COLUMN` suffix parsing. | Zero paths gives Untitled; paths open in order; `--` ends options. Treat every path literally, including colons permitted by the OS. A failed path reports an error without discarding other documents. | Dedit / D1002 CLI tests; D1005 file tests. |
| Newlines and encoding | Differ: Edit defaults to CRLF on Windows and LF elsewhere and normalizes on some write paths. | New documents use UTF-8 without BOM and LF on every OS. Opening preserves supported encoding/BOM, each line terminator and final-newline state until an explicit conversion. | Dedit file codec / D1005 byte-exact round trips. |
| Saving | Differ: the reference uses direct file creation for writes. | Write a stable snapshot via a same-directory temporary target and recoverable replacement; detect external changes. Save completion cannot mark newer edits clean. | Dedit / D1006 injected failure, race and conflict tests. |
| Dialog cancellation | Differ: Escape always means Cancel, even in an OK-only alert. | Cancel keeps documents and dirty text; a failed save also prevents close. | Widgets result + Dedit close coordinator / D1004-D1006 tests. |
| Distribution | Differ: independent GPL .NET executable named `dedit`. | Choose apphost/tool format in D1009; no NuGet library packaging assumption. | Dedit / D1009 clean install and invocation. |

## Keyboard and menu baseline

These are application bindings, not claims that every terminal can distinguish
every key combination. Menu mnemonics apply only to the open menu level. A direct
shortcut dispatches its command without first opening a menu; for example,
`Alt+V`, then plain `F` selects Go to File, while `Ctrl+P` invokes that command
directly. If a terminal does not report a chord distinctly, the menu remains
the accessible route. A modal interaction blocks background shortcuts.

| Menu/action | Mnemonic route | Direct key | Choice |
| --- | --- | --- | --- |
| File menu | Alt+F or F10, then File | — | Adopt |
| New / Open / Save | File, N / O / S | Ctrl+N / Ctrl+O / Ctrl+S | Adopt |
| Save As / Close / Exit | File, A / C / X | Ctrl+Shift+S / Ctrl+W / Ctrl+Q | Adopt; Save As key also exists in v1.2.1's application handler. |
| Save All / Close All | File, L / D | Menu only initially | Extend. |
| Edit menu | Alt+E or F10, then Edit | — | Adopt |
| Undo / Redo / Cut / Copy / Paste | Edit, U / R / T / C / P | Ctrl+Z / Ctrl+Y / Ctrl+X / Ctrl+C / Ctrl+V | Adopt |
| Find / Replace / Select All | Edit, F / L / A | Ctrl+F / Ctrl+R / Ctrl+A | Adopt |
| Find Next / Previous | Edit, N / V | F3 / Shift+F3 | Extend Previous. |
| View menu | Alt+V or F10, then View | — | Adopt |
| Status / Go to File / Go to Line / Wrap | View, S / F / G / W | menu / Ctrl+P / Ctrl+G / Alt+Z | Adopt |
| Next / previous tab | View, N / P | Ctrl+Tab / Ctrl+Shift+Tab | Extend. |
| Focus cycle | View, C | F6 / Shift+F6 | Extend; leaves an editor that consumes Tab. |
| Help / About | Alt+H, then A | F10 menu route | Adopt. |
| Indent / outdent | Editor context | Tab / Shift+Tab | Adopt. |
| Overwrite toggle | Editor context | Insert | Adopt. |
| Dismiss dialog/menu | Current scope | Escape | Explicit Cancel for dialogs. |

## Deliberate boundaries

First-release exclusions remain those in [DESIGN.md](DESIGN.md): printing,
syntax highlighting, language servers, extensions, split views, multi-caret,
tab drag reordering, and automatic session recovery. The file and coordinate
policies are in [decision records](decisions/). Released library contracts and
stage-by-stage ownership are in [Library Contracts](Library-Contracts.md).
Behavior not explicitly recorded here needs a decision and test before it
becomes a first-release promise.
