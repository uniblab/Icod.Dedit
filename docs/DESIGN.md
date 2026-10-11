# Dedit product and architecture design

Status: proposed implementation baseline, 2026-10-11.
This document describes intended behavior; it is not a claim of implemented features.

## Goal and reference

Build a practical, modeless terminal text editor with the familiar behavior of
Microsoft Edit 1.2.1, excluding printing, and with a visible tab strip for multiple
documents. Interpret the earlier reference to "exit.exe" as "edit.exe".

Use [Edit v1.2.1](https://github.com/microsoft/edit/tree/v1.2.1) as the pinned
behavioral reference, not the evolving default branch. Notepad 11.2607.14.0 is
the user's tab-interface inspiration, not a promise to reproduce every Notepad
feature or a verified specification for that exact build.

## Required product scope

- New, Open, Save, Save As, Save All, Close, Close All, and Exit.
- Multiple documents with stable identities and independent undo history, caret,
  selection, scrolling, and editing preferences.
- Visible tabs: active state, modified marker, close action, disambiguated duplicate
  names, overflow navigation, keyboard and mouse switching.
- Multiline Unicode editing: selection, cut/copy/paste, grouped undo/redo, insert
  and overwrite modes, indentation, horizontal/vertical scrolling, optional word wrap.
- Find/Replace: literal and regex search, case and whole-word options, next/previous,
  replace current/all, invalid-pattern feedback, bounded/cancellable work.
- Go to file/tab and Go to line:column.
- File/Edit/View/Help menus, contextual enabled/checked states, keyboard-first access,
  mouse interaction, and discoverable shortcuts.
- Status fields for line/column, encoding, line endings, indentation, and edit mode.
- Open/save dialogs and captioned, bordered alerts; Save / Don't Save / Cancel for
  unsaved changes. Escape always reports Cancel, including OK and Yes/No dialogs.
- Explicit encoding/BOM and line-ending handling, read-only/error states, external
  modification detection, and safe saves.

## Deliberately outside the first release

Printing of any kind; syntax highlighting; language servers; extensions; rich text;
split views; detachable windows; multi-caret editing; drag-to-reorder tabs;
automatic session/unsaved-buffer recovery; cloud/AI features.
Tabs do not imply autosave or session recovery. Revisit deferred features only
through an explicit scope decision.

## Technology and licensing

The application must be an EXE project, with `OutputType=Exe`, not `Library`
or `WinExe`. Project/root namespace: `Icod.Dedit`; executable/assembly name:
`dedit`. Use C# 13 and initially target `net8.0;net9.0;net10.0`, subject to
checking supported SDK/runtime policy before release. Tests target the same frameworks.

Use managed code and the Icod library stack. Do not add native terminal/clipboard
dependencies or duplicate terminal protocols in the editor.
The application and its tests are GPL-3.0-or-later, with the full GPL notices and
2026 Timothy J. Bruce <uniblab@hotmail.com> copyright headers requested for source
and project files. Lower-level libraries retain their existing licenses.

A .NET EXE project, a published apphost, a self-contained single-file executable,
and a NuGet .NET tool are different delivery choices. The project must run as
`dedit`; choose and test the installation/package format before configuring
release automation. Do not apply library-packaging defaults blindly.

## Responsibility boundaries

| Owner | Responsibility |
| --- | --- |
| Icod.Dedit | CLI, document identity/lifetime, tabs-to-documents binding, commands, file I/O, encoding/EOL policy, safe save/close, settings, diagnostics |
| Icod.Widgets | Reusable text document/editing model and editor view, tabs, scrollbars, forms, dialog choices, focus/command routing, selection/undo primitives |
| Icod.DCurses | Terminal-independent screen/layout projection, geometry, cursor coordination, interaction primitives and thin session capability access |
| Icod.Terminal | Terminal protocols, negotiated input, clipboard/cursor escape sequences, session lifecycle |

Dedit owns document instances; the Widgets text model must not own file paths or
perform file I/O. Document text/revisions/undo are separate from per-view
caret/selection/scroll state. The application owns the event loop and supplies
time; libraries must not create competing loops or hidden polling timers.

## Data and safety contracts

1. File byte offsets, UTF-16 offsets, grapheme boundaries, and terminal-cell columns
   are different coordinate systems. Give conversions explicit contracts.
2. Rendering, wrapping, selection, and search must never silently normalize saved
   text. Preserve tabs, mixed line endings, and final-newline state unless the
   user explicitly requests conversion.
3. Dirty state compares the current document/save-affecting metadata with its
   saved checkpoint, not "an edit has happened." Undo back to the checkpoint
   clears dirty; redo restores it. History trimming must not imply a false match.
4. Save a stable snapshot. Completing a save of revision N must not mark newer
   edits clean. Failed saves preserve both the in-memory document and recoverable
   on-disk data; never truncate the only original before success.
5. Close is a cancellable request. Cancel, Escape, or save failure keeps the
   document open. Closing several documents may save earlier ones before a later
   cancellation; do not claim those writes can be rolled back.
6. Asynchronous load/search/save/clipboard results carry document identity,
   revision where relevant, and an operation generation. Apply accepted results
   on the application loop; discard stale results and never revive a closed tab.
7. Duplicate-name display is distinct from duplicate-file identity. Define path,
   case, symlink/hard-link, and Save As collision policies explicitly.
8. Unavailable clipboard features, indistinguishable terminal shortcuts, narrow
   screens, and unsupported cursor styles degrade to usable alternatives.
9. Never send file contents or file-name control sequences directly to terminal
   output. Render nonprinting/control characters safely; saving preserves data.
10. Restore terminal modes, screen, cursor visibility/style, and input ownership
    on normal exit and handled failures.

## Interaction contract

Menu mnemonics and command shortcuts are separate: Alt+V opens View; plain F
inside it invokes Go to File, independently of Ctrl+P. F10 accesses the menu bar.
A focused find field receives its own copy/paste/undo/select-all commands rather
than having them applied to the underlying document.

Tab/Shift+Tab indent/outdent in the editor and traverse controls in forms.
Provide an explicit focus-cycle command, proposed F6/Shift+F6, so keyboard users
can leave the editor. Modal boundaries prevent background commands. Escape
dismisses/cancels the current interaction; it is never a blanket app-exit key.

Dragging a selection or scrollbar captures pointer movement until release or
cancellation, even outside the control. Focus loss, modal changes, hiding, and
disposal must release capture safely. Bracketed paste is one text transaction,
not a stream of commands. Release events must not duplicate key actions.

## Performance contract

Do not rebuild a whole-document immutable layout or allocate a full-file terminal
pad on each edit. Work from document revisions and a bounded visible-window cache.
Measure representative many-line files, very long single lines, Unicode-heavy
text, and large pastes before choosing a text-buffer data structure.

Set measured budgets for key-to-frame latency, loading, peak memory, search
cancellation, and undo retention during the foundation milestone. Do not advertise
a maximum file size or claim "unlimited" files without measured evidence.

## Acceptance and release principles

Automated document/command/file tests and deterministic input/render tests precede
live terminal qualification. Record OS build, terminal/version, architecture,
runtime, commit, and scenarios actually exercised. Windows evidence does not
prove Linux/macOS behavior. Missing environments must be marked unverified; any
release waiver is explicit and scoped to this application, not inherited from
the Widgets release.

The owner merges and tags. No roadmap task authorizes an agent to merge, publish,
modify repository settings, or edit the shared template repository.
