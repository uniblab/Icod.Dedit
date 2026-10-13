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
Alt+V distinctly, open View and press plain F for that same action. Unsaved changes require an explicit discard confirmation on exit. Supplied file paths now load into independent editable tabs;
invalid, missing and oversized paths are reported in the status row while other
files still open. Existing-file Save is available through File → Save or Ctrl+S. Save As, tab close,
and the remaining file workflows are under development; keep a separate copy
of important files while this build is being qualified.

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
planned. File → Save or Ctrl+S writes an opened file using its original
encoding and exact line endings, after checking for external changes. An
Untitled document cannot yet be saved because Save As is still pending. A
failed save leaves the buffer dirty and shows an error in the status row.
F6 and Shift+F6 cycle focus among the visible controls; Tab still indents in
the editor.
Help → About opens a captioned OK dialog; Escape cancels it. Find and Replace
commands are still scheduled for D1007.

- [Product and architecture design](docs/DESIGN.md)
- [Master implementation plan](docs/superpowers/plans/2026-10-11-dedit-master-plan.md)
- [First-release behavior matrix](docs/Behavior-Matrix.md)
- [Released library contracts](docs/Library-Contracts.md)
- [Path, text-format, and coordinate decisions](docs/decisions/)

The development branch references Icod.Widgets 1.3.1 and its Icod.DCurses
2.5.0 dependency. Widgets 1.3.1 is under review in
[PR #7](https://github.com/uniblab/Icod.Widgets/pull/7); a clean external
restore of this branch requires that package to be published.

## BUILD AND DISTRIBUTION

Run `build.cmd` on Windows or `./build.sh` on Unix-like systems with a .NET 10 SDK. Both run clean, restore, build, test, pack and a local install smoke check in Debug. On Unix-like systems, PowerShell 7 runs the shared packaging scripts; the shell script also supports the same sequence without PowerShell. An optional argument selects a stage: `clean`, `restore`, `build`, `test`, `pack` or `validate`.

The `Icod.Dedit` NuGet artifact contains one `dedit` .NET tool command with .NET 8, 9 and 10 payloads. The project remains a single EXE. Before a public release, an exact locally built package can be installed and checked with:

```sh
dotnet tool install Icod.Dedit --tool-path ./artifacts/tool --add-source ./artifacts --version 0.1.0
./artifacts/tool/dedit --version
```

On Windows, invoke `artifacts\tool\dedit.exe`. The local build scripts perform this check automatically. CI runs Staging validation on pull requests and Release validation across Windows, Linux and macOS x64/ARM64 after a merge. A `v<semver>` tag on a main-branch commit must equal the project's package version; only then does the release workflow publish the one tool package and six framework-dependent, single-file ZIP archives. No tag or release is created by a build.

The NuGet publication job requires a GitHub `Release` environment, an Actions secret `NUGET_USER`, and a NuGet.org Trusted Publishing policy for `release.yaml` in this repository. The standalone ZIP executables require the .NET 10 runtime. Local builds that run every test target need .NET 8, 9 and 10 runtimes, or an explicit runtime roll-forward policy.

## PROJECT CONTRACT

- Console application: `OutputType=Exe`, not a reusable library or GUI application.
- Repository/project identity: `Icod.Dedit`; user-facing command: `dedit`.
- Baseline: C# 13, .NET 8/9/10, managed cross-platform code.
- Planned: multiple independently editable documents, visible tabs, familiar menus and shortcuts.
- No printing, print preview, or printer dependencies.
- Saving and document lifecycle remain under development; this is not a qualified 1.0 file editor release.

## LICENSE

Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.

Icod.Dedit is free software under the GNU General Public License, version 3
or (at your option) any later version: `GPL-3.0-or-later`.
See [LICENSE](LICENSE) for the full license text.

Dependency licenses remain their own. This is an independent project, not a
Microsoft product.
