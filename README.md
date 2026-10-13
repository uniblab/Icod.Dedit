# Icod.Dedit

## NAME

`dedit` — a tabbed, modeless terminal text editor.

## DESCRIPTION

Icod.Dedit is a planned C# console executable inspired by
[Microsoft Edit 1.2.1](https://github.com/microsoft/edit/tree/v1.2.1),
with a visible, multi-document tab interface inspired by modern Windows Notepad.
Printing is deliberately excluded.

The application will use [Icod.Widgets](https://github.com/uniblab/Icod.Widgets)
and [Icod.DCurses](https://github.com/uniblab/Icod.DCurses). Reusable editing and
interaction capabilities belong in those libraries; document/file management and
editor policy belong here.

## STATUS

This repository contains the design and D1001 behavior/contract review draft,
not a working editor. The executable project, installation commands, and
release artifacts will be added during implementation.

- [Product and architecture design](docs/DESIGN.md)
- [Master implementation plan](docs/superpowers/plans/2026-10-11-dedit-master-plan.md)
- [First-release behavior matrix](docs/Behavior-Matrix.md)
- [Released library contracts](docs/Library-Contracts.md)
- [Path, text-format, and coordinate decisions](docs/decisions/)

The relevant DCurses and Widgets capabilities have been released. The D1001
decisions are proposed for owner review; the runnable EXE foundation is D1002.

## PROJECT CONTRACT

- Console application: `OutputType=Exe`, not a reusable library or GUI application.
- Repository/project identity: `Icod.Dedit`; user-facing command: `dedit`.
- Planned baseline: C# 13, .NET 8/9/10, managed cross-platform code.
- Multiple independently editable documents, visible tabs, familiar menus and shortcuts.
- No printing, print preview, or printer dependencies.
- No implementation, platform qualification, or performance claim is implied by this roadmap.

## LICENSE

Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.

Icod.Dedit is free software under the GNU General Public License, version 3
or (at your option) any later version: `GPL-3.0-or-later`.
See [LICENSE](LICENSE) for the full license text.

Dependency licenses remain their own. This is an independent project, not a
Microsoft product.
