# Icod.Dedit

## NAME

`dedit` — a tabbed, modeless terminal text editor.

## DESCRIPTION

Icod.Dedit is a C# console executable in development, inspired by
[Microsoft Edit 1.2.1](https://github.com/microsoft/edit/tree/v1.2.1),
with a visible, multi-document tab interface inspired by modern Windows Notepad.
Printing is deliberately excluded.

The application will use [Icod.Widgets](https://github.com/uniblab/Icod.Widgets)
and [Icod.DCurses](https://github.com/uniblab/Icod.DCurses). Reusable editing and
interaction capabilities belong in those libraries; document/file management and
editor policy belong here.

## STATUS

The D1001 behavior decisions are accepted. D1002 provides a runnable terminal
shell and CLI. D1003 has begun: with no file arguments, the Untitled tab is an
editable in-memory document with undo/redo. The current D1004 menu slice exposes
View → Go to File, View → Word Wrap, Edit → Undo/Redo/Cut/Copy/Paste/Select All,
and Help → About. `Ctrl+P` focuses the tab strip;
`Alt+Z` toggles wrap for the active editor. Where the terminal reports
Alt+V distinctly, open View and press plain F for that same action. Changes
are lost on exit. Supplied file paths now load into independent editable tabs;
invalid, missing and oversized paths are reported in the status row while other
files still open. Saving, close confirmation, and full file workflows remain
later milestones. Do not rely on this build to modify a file on disk.

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
Ctrl+P focuses the tab strip; Left/Right select a document, and F6 returns to
the editor. File loading accepts strict UTF-8 and BOM-marked UTF-16 LE/BE up to
32 MiB, preserving mixed line endings and the original encoding in memory.
Ctrl+Z/Ctrl+Y undo and redo in Untitled; Ctrl+X/C/V/A cut, copy, paste and
select all using an in-process clipboard; Ctrl+Q exits after confirmation when
any document is modified. Escape cancels that confirmation. Copy and cut currently
require a selection. Terminal clipboard transport and linewise operations are
planned. No document is saved.
F6 and Shift+F6 cycle focus among the visible controls; Tab still indents in
the editor.
Help → About opens a captioned OK dialog; Escape cancels it. File and the
remaining Find and Replace commands are still scheduled for D1004/D1007.

- [Product and architecture design](docs/DESIGN.md)
- [Master implementation plan](docs/superpowers/plans/2026-10-11-dedit-master-plan.md)
- [First-release behavior matrix](docs/Behavior-Matrix.md)
- [Released library contracts](docs/Library-Contracts.md)
- [Path, text-format, and coordinate decisions](docs/decisions/)

The development branch references Icod.Widgets 1.3.1 and its Icod.DCurses
2.5.0 dependency. Widgets 1.3.1 is under review in
[PR #7](https://github.com/uniblab/Icod.Widgets/pull/7); a clean external
restore of this branch requires that package to be published.

## PROJECT CONTRACT

- Console application: `OutputType=Exe`, not a reusable library or GUI application.
- Repository/project identity: `Icod.Dedit`; user-facing command: `dedit`.
- Baseline: C# 13, .NET 8/9/10, managed cross-platform code.
- Planned: multiple independently editable documents, visible tabs, familiar menus and shortcuts.
- No printing, print preview, or printer dependencies.
- The in-memory editor is a development slice, not a qualified file editor release.

## LICENSE

Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.

Icod.Dedit is free software under the GNU General Public License, version 3
or (at your option) any later version: `GPL-3.0-or-later`.
See [LICENSE](LICENSE) for the full license text.

Dependency licenses remain their own. This is an independent project, not a
Microsoft product.
