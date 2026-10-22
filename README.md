# dedit(1)

## NAME

`dedit` — edit one or more text files in a tabbed terminal interface.

## SYNOPSIS

```text
dedit [OPTION]... [--] [FILE]...
dedit --help
dedit --version
```

## DESCRIPTION

Icod.Dedit is a modeless terminal text editor for .NET. It combines a
multi-document tab interface with keyboard and pointer operation, Unicode-aware
editing, search and replace, optional word wrap, line numbers, and safe file
saving. Its interaction is inspired by Microsoft Edit and modern Windows
Notepad. Printing is deliberately excluded.

Each distinct resolved `FILE` path is opened in its own tab, in command-line
order. A repeated path or filesystem alias activates the tab already associated
with that file. An invocation without files starts with one editable `Untitled`
document. A path that cannot be opened produces a status diagnostic without
preventing other valid paths from opening.

Closing the final tab leaves a blank workspace. File, Edit, and View commands
that require a document are then disabled; create or open a document to restore
the editor.

## INSTALLATION

Install the current release as a global .NET tool:

```sh
dotnet tool install --global Icod.Dedit
```

Run it by command name:

```sh
dedit
```

Update an existing installation with:

```sh
dotnet tool update --global Icod.Dedit
```

The package contains tool payloads for .NET 8, 9, and 10. A compatible .NET
runtime must be installed.

## OPTIONS

| Option | Description |
|---|---|
| `-h`, `--help` | Print command-line usage and exit. |
| `-v`, `--version` | Print the program name and version, then exit. |
| `--` | End option processing. All following arguments are treated as file paths. |

Options take effect when encountered. Use `--` before a file name beginning
with `-`. Colons in paths are treated literally; `FILE:LINE:COLUMN` syntax is
not interpreted.

## USER INTERFACE

The screen contains a menu bar, a document tab strip, the editor, and a status
row. The active tab retains its own text, caret, selection, view, indentation,
encoding, and undo history.

The editor shows fixed line numbers in a decorative left gutter and marks the
current logical line with a theme-aware band. Wrapped continuation rows are not
numbered. Gutter clicks are ignored. If the viewport is too narrow for both the
gutter and text, the text receives the full width.

The status row reports the document name, one-based logical line and character
position, encoding, current line ending, indentation width, selection state,
and insert or overwrite mode. Character positions count Unicode graphemes, so
a composed character, tab, or emoji counts as one position. To keep status
updates bounded on extremely long lines, a caret more than 4,096 UTF-16 units
from the line start is reported as `Col >4096`.

Auto-hidden vertical and horizontal scrollbars appear only when their content
exceeds the viewport. The horizontal scrollbar is limited to no-wrap mode.
Vertical scrolling follows displayed rows, including wrapped continuations,
while the status row continues to report logical line and character position.

## KEYBOARD COMMANDS

### Menus and focus

| Key | Action |
|---|---|
| `Alt+F` | Open the File menu. |
| `Alt+E` | Open the Edit menu. |
| `Alt+V` | Open the View menu when the terminal reports this key distinctly. |
| `Alt+H` | Open the Help menu. |
| `F10` | Focus the menu bar. |
| Arrow keys | Move within an open menu or submenu. |
| `Enter` | Invoke the selected menu item or dialog action. |
| `Escape` | Close the current menu or cancel the current dialog. |
| `F6`, `Shift+F6` | Cycle forward or backward through visible controls. |
| `Ctrl+P` | Focus the tab strip; use Left and Right to select a document. |

When a menu is open, press the highlighted mnemonic letter to invoke an item
or open a submenu. Menu commands, direct shortcuts, and pointer clicks use the
same command paths.

### File commands

| Key | Menu command | Action |
|---|---|---|
| `Ctrl+N` | File → New | Open a new `Untitled` tab. |
| `Ctrl+O` | File → Open | Open the multi-file picker. |
| `Ctrl+S` | File → Save | Save the active document; an Untitled document opens Save As. |
| `Ctrl+Shift+S` | File → Save As | Choose a new directory and file name. |
| — | File → Save All | Save dirty documents in tab order. |
| `Ctrl+W` | File → Close | Close the active document. |
| — | File → Close All | Close documents in tab order. |
| `Ctrl+Q` | File → Exit | Close the workspace and return to the invoking shell. |

### Editing and search

| Key | Menu command | Action |
|---|---|---|
| `Ctrl+Z` | Edit → Undo | Undo the most recent edit in the active tab. |
| `Ctrl+Y` | Edit → Redo | Redo the most recently undone edit. |
| `Ctrl+X` | Edit → Cut | Cut the selection, or the current logical line when nothing is selected. |
| `Ctrl+C` | Edit → Copy | Copy the selection, or the current logical line when nothing is selected. |
| `Ctrl+V` | Edit → Paste | Replace the selection or insert clipboard text. |
| `Ctrl+A` | Edit → Select All | Select the entire document. |
| `Ctrl+F` | Edit → Find | Open the Find dialog. |
| `Ctrl+R` | Edit → Replace | Open the Replace dialog. |
| `F3` | Edit → Find Next | Repeat the last search forward. |
| `Shift+F3` | Edit → Find Previous | Repeat the last search backward. |

### Navigation and view

| Key | Menu command | Action |
|---|---|---|
| Arrow keys | — | Move by Unicode grapheme or displayed row. |
| `Ctrl+Left`, `Ctrl+Right` | — | Move by word. |
| `Home`, `End` | — | Move to the start or end of the logical line. |
| `Ctrl+Home`, `Ctrl+End` | — | Move to the start or end of the document. |
| `Page Up`, `Page Down` | — | Move through the document by displayed pages. |
| `Shift` plus a movement key | — | Extend the selection. |
| `Ctrl+Up`, `Ctrl+Down` | — | Scroll by one displayed row, carrying the caret at the viewport edge when necessary. |
| `Ctrl+G` | View → Go to Line:Column | Move to a one-based logical line and grapheme position. |
| `Alt+Z` | View → Word Wrap | Toggle wrapping for the active tab. |
| `Tab` | — | Insert spaces, or indent every selected logical line, using the active tab's indentation width. |
| `Shift+Tab` | — | Outdent the current or selected logical lines. |
| `Insert` | — | Toggle insert and overwrite mode. |

Vertical movement across wrapped text retains the caret's preferred visual
column. Go to Line:Column accepts two one-based numbers separated by a colon,
for example `139:24`.

### Menu-only settings

| Menu command | Action |
|---|---|
| File → Encoding | Select UTF-8, UTF-8 BOM, UTF-16 LE, or UTF-16 BE for the next save. |
| File → Line Endings | Convert the document to Unix LF, Windows CRLF, or Classic Mac CR. |
| File → Indentation | Select 2, 4, or 8 spaces for future Tab input. |
| Edit → Match Case | Toggle invariant case-sensitive searching. |
| Edit → Whole Word | Toggle whole-word searching. |
| Edit → Regular Expression | Toggle .NET regular-expression searching. |
| Help → About dedit | Display the program version, copyright, and license. |

## MOUSE SUPPORT

Menus, tabs, file pickers, dialog actions, the editor, and scrollbars accept
pointer input when the terminal provides it. Clicking in the editor places the
caret; dragging extends the selection.

Scrollbar behavior follows conventional GUI controls:

- `▲` and `▼` scroll one displayed row; `◄` and `►` scroll one column.
- Clicking the track pages toward the pointer.
- Dragging the thumb scrolls proportionally through the document.
- The pointer wheel scrolls vertically.
- Right-clicking a scrollbar opens its standard scroll-command menu.

Modifier-click direct positioning is not supported. Shift-click and
Control-click on the track retain ordinary paging behavior.

## CLIPBOARD

Explicit Copy and Cut write to a bounded process-local clipboard and also
attempt to write the terminal clipboard. Paste requests the terminal clipboard
with a 250 ms limit, then uses the process-local text if the terminal query is
unavailable. Clipboard text is limited to 64 KiB of UTF-8 data.

With no selection, Copy and Cut operate on the current logical line and retain
its exact terminator. Pasting a locally copied line inserts it at the start of
the destination line. A different terminal clipboard value is pasted as plain
text. If pasted text is identical to an active selection, the selection
collapses at its endpoint without changing the document or adding undo history.

## SEARCH AND REPLACE

Find and Replace support forward and backward searches, optional invariant
case matching, whole-word matching, and .NET regular expressions. Searches
wrap once within the active tab. Replace All is recorded as one undoable edit.
Invalid expressions, expired work limits, and results made stale by a document
change leave the document unchanged.

## FILE HANDLING

Dedit opens strict UTF-8, UTF-8 with BOM, UTF-16 little-endian with BOM, and
UTF-16 big-endian with BOM. Files containing invalid text or NUL characters are
rejected. The maximum file size is 32 MiB.

The original encoding, mixed line endings, and presence or absence of a final
line terminator are preserved in memory and on an ordinary save. File →
Encoding changes the encoding for the next save. File → Line Endings converts
the whole document to LF, CRLF, or CR as one undoable edit. File → Indentation
selects 2, 4, or 8 spaces for future Tab input without rewriting existing text.

Saves use a temporary file in the destination directory and verify both the
path target and original content before replacement. Read-only destinations,
external changes, retargeted paths, and a path owned by another open tab are
refused rather than overwritten silently.

Saving over an existing Save As destination requires a separate confirmation
with No selected by default. If an opened file changes externally, Yes reloads
the disk copy and No keeps the editor copy; keeping it leaves saving to the
original path blocked until the conflict is resolved. Use Save As to preserve
the editor copy under another name.

A dirty Close, Close All, or Exit operation asks `Yes / Don't Save / Cancel`
for each affected document. `Y/N/C` activate those choices, with
Cancel selected by default. Cancel stops the remaining operation; earlier successful
saves remain. Clean documents close without a prompt.

## PREFERENCES

At startup, dedit optionally reads `Icod.Dedit/preferences.json` beneath the
operating system's application-data directory. The version 1 format is:

```json
{
  "version": 1,
  "tabWidth": 4,
  "indentWidth": 4,
  "wrap": false,
  "defaultEncoding": "utf-8"
}
```

| Property | Accepted values | Default | Scope |
|---|---|---|---|
| `version` | `1` | required | File format version. |
| `tabWidth` | `1` through `16` | `4` | Display width of tab characters. |
| `indentWidth` | `1` through `16` | `4` | Initial spaces inserted by Tab in each opened tab. |
| `wrap` | `true`, `false` | `false` | Initial wrapping state. |
| `defaultEncoding` | `utf-8`, `utf-8-bom`, `utf-16-le`, `utf-16-be` | `utf-8` | Encoding of new Untitled documents. |

Existing files retain their detected encoding. An inaccessible, malformed,
unknown-version, or larger-than-16-KiB preferences file is ignored in full and
the defaults above are used. Preferences are read only at startup and are not
modified by menu commands or document saves.

## ENVIRONMENT

Dedit requires an interactive terminal. Color, modified-key reporting,
terminal clipboard access, pointer reporting, and caret shapes depend on the
capabilities reported by the terminal. On color terminals, selection uses an
explicit contrasting background and the status row shows `SEL`; monochrome
terminals use an attribute-based fallback.

## EXIT STATUS

| Status | Meaning |
|---|---|
| `0` | Normal exit, or successful `--help` or `--version`. |
| `1` | The editor could not start or complete because of an I/O, access, terminal, or other supported runtime error. |
| `2` | Invalid command-line arguments. |

## EXAMPLES

Start with a new Untitled document:

```sh
dedit
```

Open several files in separate tabs:

```sh
dedit README.md src/Program.cs "notes with spaces.txt"
```

Open a file whose name begins with a hyphen:

```sh
dedit -- -draft.txt
```

Display the installed version:

```sh
dedit --version
```

## BUILDING FROM SOURCE

A .NET 10 SDK builds all three target frameworks. On Windows run `build.cmd`;
on Unix-like systems run `./build.sh`. With no argument, each script performs
clean, restore, build, test, pack, and local tool validation in Debug. Pass one
of `clean`, `restore`, `build`, `test`, `pack`, or `validate` to run only that
stage. Except for `clean` and `restore`, a selected stage expects its earlier
restore or build outputs to exist.

To run directly from the source tree:

```sh
dotnet run --project Icod.Dedit.csproj -f net10.0 -- [FILE ...]
```

## FILES

| File | Purpose |
|---|---|
| `Icod.Dedit/preferences.json` under the application-data directory | Optional startup preferences. |
| `README.md` | User manual. |
| `CHANGELOG.md` | Complete feature and release history. |

## LIMITATIONS

- Dedit edits supported text encodings only; invalid text and files containing
  NUL characters are rejected.
- Documents larger than 32 MiB and clipboard values larger than 64 KiB of
  UTF-8 data are not accepted.
- Printing, print preview, and printer integration are not provided.
- Preferences are startup defaults, not an in-application settings store.
- Terminal capabilities can limit Alt-key, pointer, clipboard, color, and
  caret-shape behavior.

## SEE ALSO

- [Implemented changes](CHANGELOG.md)
- [1.0.0 release notes](docs/releases/1.0.0.md)
- [Product and architecture design](docs/DESIGN.md)
- [First-release behavior matrix](docs/Behavior-Matrix.md)
- [Released library contracts](docs/Library-Contracts.md)
- [Path, text-format, and coordinate decisions](docs/decisions)
- [Master implementation plan](docs/superpowers/plans/2026-10-11-dedit-master-plan.md)

Dedit is built on [Icod.Widgets](https://github.com/uniblab/Icod.Widgets) and
[Icod.DCurses](https://github.com/uniblab/Icod.DCurses). It is an independent
project and is not a Microsoft product.

## REPORTING BUGS

Report reproducible problems through the
[Icod.Dedit issue tracker](https://github.com/uniblab/Icod.Dedit/issues). Include
the operating system, terminal, .NET version, dedit version, and the shortest
sequence that reproduces the behavior.

## AUTHOR

Written by Timothy J. Bruce.

## LICENSE

Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.

Icod.Dedit is free software under the GNU General Public License, version 3 or,
at your option, any later version (`GPL-3.0-or-later`). See [LICENSE](LICENSE)
for the full license text. Dependency licenses remain their own.
