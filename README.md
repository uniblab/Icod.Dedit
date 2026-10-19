# Icod.Dedit

## NAME

`dedit` — a tabbed, modeless terminal text editor.

## DESCRIPTION

Icod.Dedit is a C# console executable in development, inspired by
[Microsoft Edit 1.2.1](https://github.com/microsoft/edit/tree/v1.2.1),
with a visible, multi-document tab interface inspired by modern Windows Notepad.
Printing is deliberately excluded.

The application uses [Icod.Widgets](https://github.com/uniblab/Icod.Widgets)
and [Icod.DCurses](https://github.com/uniblab/Icod.DCurses). Reusable editing and
interaction capabilities belong in those libraries; document/file management and
editor policy belong here.

## STATUS

The D1001 behavior decisions are accepted. The current development build has
editable in-memory and file-backed tabs with independent undo history. File,
Edit, View, and Help menus expose New, Open, Save, Save As, Save All, Close,
Close All, Exit, Undo/Redo, Cut/Copy/Paste/Select All, Find, Replace,
Go to File, Go to Line:Column, Word Wrap, and About.
`Ctrl+P` focuses the tab strip and `Alt+Z` toggles wrap for the active
editor. Where the terminal reports Alt+V distinctly, open View and press plain
F to focus the tab strip.

Supplied file paths load into independent tabs. Invalid, missing, and oversized
paths produce a status diagnostic while other files still open. Save As accepts
a new path or an existing file. Replacing an existing file requires a separate
confirmation with No selected by default; a changed destination or one owned by
another open tab is refused. Save All processes dirty tabs in order and pauses
for Untitled Save As. Close All and Exit ask Yes / Don't Save / Cancel for
each dirty tab. Cancel stops later work while earlier successful saves remain.
Externally changed files offer an explicit Reload or Keep decision when a tab
is activated or an active save finds a conflict. The default Keep choice
preserves the buffer; saving to the original path remains blocked until the
conflict is resolved. Reload discards unsaved changes only after confirmation.
Search, coordinate navigation, encoding, line endings and per-tab indentation
controls are available. Release qualification remains under development.
Build with a .NET 10 SDK (which targets .NET 8, 9, and 10) and run in an
interactive terminal with the selected runtime installed:

```sh
./build.sh
dotnet run --project Icod.Dedit.csproj -f net10.0 -- --help
dotnet run --project Icod.Dedit.csproj -f net10.0 -- "first file.txt" "second.txt"
```

On Windows, use `build.cmd` and the same `dotnet run` commands. The shell
shows one editable Untitled tab without paths and loaded documents in argument
order when paths are supplied. Use `--` before a path beginning with `-`.
Ctrl+N opens another Untitled tab. Ctrl+O opens a file picker that accepts
multiple files. Ctrl+P focuses the tab strip; Left/Right select a document,
and F6 returns to the editor. File loading accepts strict UTF-8 and BOM-marked UTF-16 LE/BE up to
32 MiB, preserving mixed line endings and the original encoding in memory.
Ctrl+Z/Ctrl+Y undo and redo; Ctrl+X/C/V/A cut, copy, paste, and select
all using a bounded process-local clipboard. Explicit copy/cut also attempt
to write the terminal clipboard; explicit paste requests it with a 250 ms
limit and uses the process-local text when the terminal query is unavailable.
With no selection, Copy/Cut uses the current logical line and its exact
terminator. A local linewise Paste inserts at the current line start; selected
text is replaced normally. A different terminal clipboard value pastes as plain
text. Pasting text identical to an active selection completes by collapsing the
selection at its endpoint without creating a document edit. When an
unterminated copied line is inserted before existing text, Paste adds the
editor's current newline as a separator.
File → Save or Ctrl+S opens Save As for Untitled, or writes an opened file
using its original encoding and exact line endings after checking for external
changes. File → Save As or
Ctrl+Shift+S opens a directory and name picker. An existing file triggers an
additional No-default overwrite confirmation; Escape or No leaves it untouched.
The destination's identity and contents are checked again before replacement.
A failed save leaves the buffer dirty and shows an error in the status row.
If another process changed the file, a Reload/Keep prompt lets you load the
disk copy or retain the editor copy. Keep the latter with Save As under a
different path; No is the default, and Escape cancels the prompt.
File → Save All visits dirty documents in tab order. File → Encoding selects
strict UTF-8, UTF-8 BOM, UTF-16 LE or UTF-16 BE for the next save; a format
change marks the tab dirty. File → Line Endings explicitly converts mixed
terminators to LF, CRLF or CR as one undoable edit and preserves whether the
file ends in a terminator. Future Enter keys use the selected ending; opening a
file uses its first existing terminator for future Enter keys.

File → Indentation selects 2, 4 or 8 spaces for future Tab indentation in
the active tab. Changing the setting leaves existing text and save state intact.

Ctrl+W closes the active tab after Yes / Don't Save / Cancel when it is dirty.
The close control on each tab uses the same decision: Y/N/C are the shortcuts,
with Cancel selected by default; clean documents close without a prompt. File →
Close All visits tabs in order. File → Exit or Ctrl+Q uses the
same per-tab decisions and exits only when the sequence completes. Closing the
last tab leaves a blank workspace with document commands disabled; Ctrl+N or a
successful Ctrl+O open restores the editor. Starting without paths still
creates one Untitled document.
F6 and Shift+F6 cycle focus among the visible controls; Tab still indents in
the editor.
Ctrl+F opens Find and Ctrl+R opens Replace. F3 and Shift+F3 repeat the last
search in the current tab, wrapping once. The Edit menu toggles case matching,
whole-word matching, and .NET regular expressions. Replace All is one undoable
edit. Invalid patterns and stale results leave the document unchanged.
Ctrl+G opens Go to Line:Column. Enter two one-based numbers separated by a
colon, such as `2:5`; columns count Unicode graphemes, including tabs and
emoji as one position each. Invalid or out-of-range positions stay in the
form. Help → About opens a captioned OK dialog; Escape cancels it.
Menus, tabs, picker and dialog actions accept pointer clicks; dragging in the
editor extends the text selection. On color terminals, selected text uses an
explicit blue background instead of reverse video, the status row shows `SEL`,
and a bar/block caret distinguishes insert and overwrite mode.
The editor shows fixed line numbers in a decorative left gutter and marks the
current logical line with a theme-aware band. Wrapped continuation rows remain
unnumbered, gutter clicks leave the caret and selection unchanged, and a
viewport too narrow for both gutter and text gives the full width to text.
Vertical movement traverses wrapped screen rows while retaining its preferred
visual column; the status row continues to report logical line and character.

- [Product and architecture design](https://github.com/uniblab/Icod.Dedit/blob/main/docs/DESIGN.md)
- [Master implementation plan](https://github.com/uniblab/Icod.Dedit/blob/main/docs/superpowers/plans/2026-10-11-dedit-master-plan.md)
- [First-release behavior matrix](https://github.com/uniblab/Icod.Dedit/blob/main/docs/Behavior-Matrix.md)
- [Implemented changes](CHANGELOG.md)
- [Released library contracts](https://github.com/uniblab/Icod.Dedit/blob/main/docs/Library-Contracts.md)
- [Path, text-format, and coordinate decisions](https://github.com/uniblab/Icod.Dedit/tree/main/docs/decisions)

## PREFERENCES

At startup, dedit reads `Icod.Dedit/preferences.json` under the operating
system's application-data directory when it exists. This optional version 1
file sets defaults for newly opened tabs:

```json
{"version":1,"tabWidth":4,"indentWidth":4,"wrap":false,"defaultEncoding":"utf-8"}
```

`tabWidth` and `indentWidth` accept 1–16. `defaultEncoding` accepts
`utf-8`, `utf-8-bom`, `utf-16-le`, or `utf-16-be`; it applies to new
Untitled tabs only. Existing files retain their detected encoding. A malformed,
unknown-version, inaccessible, or oversized preferences file uses the defaults
shown above. Preferences are read only at startup and are kept separate from
document saves. The File → Encoding and File → Indentation commands affect the active tab;
View → Word Wrap affects the active view.

## BUILD AND DISTRIBUTION

Run `build.cmd` on Windows or `./build.sh` on Unix-like systems with a .NET 10 SDK. Both run clean, restore, build, test, pack and a local install smoke check in Debug. On Unix-like systems, PowerShell 7 runs the shared packaging scripts; the shell script also supports the same sequence without PowerShell. An optional argument selects a stage: `clean`, `restore`, `build`, `test`, `pack` or `validate`.

The `Icod.Dedit` NuGet artifact contains one `dedit` .NET tool command with .NET 8, 9 and 10 payloads. The project remains a single EXE. Before a public release, an exact locally built package can be installed and checked with:

```sh
dotnet tool install Icod.Dedit --tool-path ./artifacts/tool --add-source ./artifacts --version 1.0.0
./artifacts/tool/dedit --version
```

On Windows, invoke `artifacts\tool\dedit.exe`. The local build scripts perform this check automatically. CI runs Staging validation on pull requests and Release validation across Windows, Linux and macOS x64/ARM64 after a merge. A `v<semver>` tag on a main-branch commit must equal the project's package version; only then does the release workflow publish the one tool package and six framework-dependent, single-file ZIP archives. No tag or release is created by a build.

The NuGet publication job requires a GitHub `Release` environment, an Actions secret `NUGET_USER`, and a NuGet.org Trusted Publishing policy for `release.yaml` in this repository. The standalone ZIP executables require the .NET 10 runtime. Local builds that run every test target need .NET 8, 9 and 10 runtimes, or an explicit runtime roll-forward policy.

## PROJECT CONTRACT

- Console application: `OutputType=Exe`, not a reusable library or GUI application.
- Repository/project identity: `Icod.Dedit`; user-facing command: `dedit`.
- Baseline: C# 13, .NET 8/9/10, managed cross-platform code.
- Implemented in this development branch: independent editable tabs and the listed file/menu commands.
- No printing, print preview, or printer dependencies.
- The current build has not completed 1.0 qualification or all filesystem edge-case checks.

## LICENSE

Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.

Icod.Dedit is free software under the GNU General Public License, version 3
or (at your option) any later version: `GPL-3.0-or-later`.
See [LICENSE](LICENSE) for the full license text.

Dependency licenses remain their own. This is an independent project, not a
Microsoft product.
