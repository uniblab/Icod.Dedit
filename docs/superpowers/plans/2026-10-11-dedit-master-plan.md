# Dedit Master Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for approved
> implementation tasks. Do not begin implementation from this master roadmap:
> first review it with the owner and complete the DCurses/Widgets capability
> designs. The libraries have since shipped the relevant contracts and the owner
> approved D1001's behavior decisions. Work sequentially
> unless the owner requests another execution method.
> Checkboxes record future work, not completed features.

**Goal:** Deliver `dedit`, a GPL-3.0-or-later console executable with Microsoft
Edit 1.2.1-style editing and visible multi-document tabs, without printing.

**Architecture:** Dedit owns the application loop, document/file lifecycle, and
editor commands. Widgets supplies reusable editing and UI behavior; DCurses and
Terminal supply geometry, rendering, input, and terminal capabilities. Develop
small end-to-end slices against released library contracts rather than copying
library internals into the application.

**Tech Stack:** C# 13; .NET 8/9/10; Icod.Widgets; Icod.DCurses; the existing
Icod.Terminal integration; xUnit tests.

**Spec:** [Product and architecture design](../../DESIGN.md).

**Current progress:** The owner accepted the [D1001 behavior matrix](../../Behavior-Matrix.md),
[decision records](../../decisions/) and [released library contracts](../../Library-Contracts.md).
D1002 supplies an executable shell and lifecycle foundation. D1003 has begun
with an in-memory Untitled editor. File loading and later milestones remain.

## Global constraints

- Console application: `OutputType=Exe`, never `Library` or `WinExe`.
- Project/root namespace: `Icod.Dedit`; executable/assembly name: `dedit`.
- Initial targets: `net8.0;net9.0;net10.0`; language version `13.0`.
- Application, tests, and project notices: `GPL-3.0-or-later`.
- Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.
- Full prescribed GPL notices at the beginning of every authored C# file;
  XML declaration and corresponding GPL comment headers at the beginning of
  project files. Generated build output is not checked in.
- No print support, native terminal dependencies, or editor-owned terminal protocols.
- Escape always returns Cancel from dialogs, including OK and Yes/No.
- The owner merges and tags. Applying a workflow template means changing this
  repository, not opening a PR in `uniblab/.github`.
- This PR contains the D1002 executable shell and roadmap. Reusable library
  implementation remains in its owning repositories when required.

## Review focus

Each risk has a specific acceptance test in the owning milestone.

1. Save/close races and failed writes must not lose text or clear newer edits (D1006).
2. Unicode clusters, mixed EOLs, and giant lines must not corrupt movement or bytes (D1003/D1005).
3. Modal focus, ambiguous shortcuts, and paste must not invoke background commands (D1004).
4. Duplicate paths and late work must not replace another document or revive a tab (D1005/D1006).
5. Tiny terminals, control characters, and shutdown failures must not corrupt the terminal (D1008).

## Proposed repository map

These are planned paths, not existing implementations. Keep the application in
one EXE project initially; do not create a speculative application framework.

| Path | Responsibility |
| --- | --- |
| `Icod.Dedit.sln`, `Icod.Dedit.csproj` | Executable and test build, target/runtime/package metadata |
| `src/Program.cs` | Entry point, exit code and top-level cleanup |
| `src/Application/DeditApplication.cs` | Explicit event loop and UI composition |
| `src/Application/CommandRouter.cs` | Contextual application command dispatch/state |
| `src/Documents/DocumentManager.cs` | Stable IDs, active document, ordered open documents |
| `src/Documents/EditorDocument.cs` | Widgets document model plus file/save metadata |
| `src/Documents/DocumentViewState.cs` | Caret, selection, scroll and display preferences |
| `src/Files/DocumentFileService.cs` | Read/write snapshots, conflict checks, recoverable replacement |
| `src/Files/TextFileCodec.cs` | Encoding/BOM/EOL-preserving decode/encode |
| `src/Interaction/CloseCoordinator.cs` | Save/discard/cancel state machine |
| `src/Interaction/SearchController.cs` | Search lifetime, matches, replacement transactions |
| `src/Presentation/EditorShell.cs` | Menus, tabs, editor view, status, overlays |
| `src/CommandLine/DeditOptions.cs` | Argument parsing and usage |
| `src/Settings/EditorSettings.cs` | Versioned, validated preferences |
| `tests/Icod.Dedit.Tests/` | Unit, filesystem, interaction, and process smoke tests |
| `docs/Behavior-Matrix.md` | Reference behavior, deliberate differences, acceptance mapping |
| `docs/Qualification.md` | Per-commit measured and live-terminal evidence |
| `docs/decisions/` | Narrow decisions such as encoding policy and distribution |
| `build.cmd`, `build.sh`, `.github/workflows/` | Reproducible build/test/package workflow |
| `README.md`, `CHANGELOG.md` | Durable usage and actual implemented history |

Exclude tests and any future samples from root-project compile globs. A test
project reference to the EXE does not require converting the application to a library.

Public library type names below are capability labels, not approved API signatures.
Each library design must provide exact ownership, disposal, event, and threading
contracts before a consuming task is expanded into a code-level implementation plan.

## Milestones and dependency order

| ID | Deliverable | Depends on |
| --- | --- | --- |
| D1001 | Accepted behavior matrix and library capability contracts | This planning review |
| D1002 | Buildable EXE and clean terminal lifecycle | D1001 baseline |
| D1003 | One-document editor with measured text foundation | D1002; editor/input/layout library slice |
| D1004 | Contextual commands, forms and accessible navigation | D1003; routing/dialog slice |
| D1005 | Real files and multi-document tabs | D1003/D1004; tabs/picker slice |
| D1006 | Safe save/close/quit workflows | D1005 |
| D1007 | Find/replace and file-format editing | D1006 |
| D1008 | Robustness, performance and qualification | D1007 |
| D1009 | Distribution, documentation and release readiness | D1008 |

Library work need not all finish before Dedit starts. Agree contracts first,
then release and consume the smallest coherent library slice required by each
milestone. Do not ship temporary application copies of reusable widget code.

## D1001 — Lock behavior and scope; design dependencies next

**Files:** `docs/Behavior-Matrix.md`, `docs/decisions/`, updates to this plan.
**Output:** reviewed acceptance matrix and links to separate library plans.

- [x] Inventory the pinned Edit 1.2.1 menus, navigation, selection, indentation,
  overwrite, search, clipboard and CLI behavior. Mark each as adopt, deliberately
  differ, or defer; do not silently follow newer Edit versions.
- [x] Record default shortcuts and menu mnemonics separately. Baseline: Ctrl+N/O/S/W/Q,
  Ctrl+Z/Y/X/C/V/A, Ctrl+F, Ctrl+R, Ctrl+P, Ctrl+G, Alt+Z, F10. Propose Ctrl+Tab /
  Ctrl+Shift+Tab for tab switching and F6 / Shift+F6 for focus cycling, with visible
  menu alternatives when terminals cannot distinguish those chords.
- [x] Specify selection extension, word movement, Home/End, preferred vertical column,
  click counts, drag/autoscroll, insert/overwrite, linewise copy without selection,
  tab width and indentation defaults. Make deliberate differences explicit.
- [x] Define path identity, supported encodings, default newline/encoding, display
  column semantics, regex semantics, and close-after-last-tab behavior in small
  decision records. Require an empty untitled editing surface after the last
  ordinary tab closes; Exit remains a separate command.
- [x] Review the separately delivered DCurses and Widgets capability designs
  against published 2.5.0 and 1.3.0 contracts. The application integration
  map is in [Library Contracts](../../Library-Contracts.md); future gaps remain
  in their owning libraries.
- [x] Review the behavior matrix and boundary decisions with the owner before
  implementation. Commit the accepted baseline independently of runtime code.

**Acceptance:** every first-release feature has an owner and an observable test;
no unresolved safety policy is hidden behind a UI implementation task.

## Library capability backlog for the next discussion

The prior source assessment used Widgets commit
`1645c25aeaa90342c3dcc97e442c8b449d646f7d`, DCurses commit
`e8fe56faaee25e4fe45ce74eab034bfa8fc68905`, and Terminal commit
`2c410cd80663fef30912079e2c0de7a550852b97`.
Recheck current branches/releases before designing changes; these are historical
inspection points, not required dependency pins.

| Owner / capability | Required behavior and design question |
| --- | --- |
| Widgets input mapping | Preserve function-key number, press/repeat/release phase, bracketed-paste phase and terminal focus. Lower-layer data already exists; avoid inventing new terminal protocols. |
| Widgets focus/commands | Let the focused editor consume Tab; form traversal still works. Route editing commands to the focused control; honor modal scopes and dynamic menu enabled/checked state. |
| Widgets pointer capture | Deliver drag motion/release to the capturing control; cancel on focus loss, removal or modal transition. Reuse applicable DCurses routing primitives. |
| Widgets document/editor | Reusable efficient text model, indexed lines, revision/snapshot events, grouped bounded undo, saved checkpoints, Unicode selection and independent view state. Evaluate buffer structures rather than choosing by habit. |
| Widgets tab control | Stable IDs, active/dirty states, cancellable close requests, overflow, disambiguated labels, mouse and keyboard operation. The control never saves or disposes a document on its own. |
| Widgets scrollbars | Interactive thumb/page/wheel, capture and small-range/large-range behavior; an existing percentage indicator is not a scrollbar. |
| Widgets forms/dialogs | Public composition, validation, default action, focus traversal; caption/border and configurable Save / Don't Save / Cancel choices. |
| Widgets save picker | Select a new or existing target with overwrite confirmation; return a path without writing it. Do not weaken existing-file picker validation. |
| Widgets status/search UI | Focusable status fields and composable find/replace UI; improve shared single-line selection/clipboard/undo primitives without introducing multiline behavior into single-line fields. |
| DCurses layout/projection | Support measured viewport-local text projection, hit testing, selection/caret overlays and invalidation. Determine which caching belongs in Widgets and which missing geometry primitives genuinely belong in DCurses. |
| DCurses session facade | Reuse Terminal clipboard read/write and scoped cursor-style capabilities. Expose only missing safe access with cancellation, bounds and fallback; do not reimplement OSC protocols. |
| DCurses interaction timing | Caller-fed timing for autoscroll/repeat/click recognition where current contracts require extension; no independent UI loop. |

Existing DCurses text layout already supplies Unicode geometry, source/visual
mapping, selection rectangles and partial-row presentation. Its immutable,
bounded whole-string representation must not become a full-document rebuild
on every keypress. Prototype/cache/measure before proposing a new public API.
Existing Terminal clipboard and cursor-style support should be reused.

**Library exit gate:** unit/regression tests, runnable samples, API documentation,
backward-compatibility review, and available package versions for each required
slice. Do not label proposed controls as already available.

## D1002 — Executable skeleton and lifecycle

**Files:** solution/project, `src/Program.cs`, `src/Application/DeditApplication.cs`,
`src/CommandLine/DeditOptions.cs`, tests, build scripts.

- [x] Add failing CLI/process tests: help and version do not initialize a terminal;
  invalid arguments produce a diagnostic and nonzero status.
- [x] Create the EXE/test projects with the specified targets, names, license
  metadata and full file headers. Select released library versions explicitly.
- [x] Build an empty shell with one owner for the session and an injectable event
  source/clock; prove normal exit and handled startup failures release resources.
- [x] Establish command-line contract: zero paths opens Untitled, multiple paths
  open in argument order, `--` terminates options, and quoted/spaced/non-ASCII
  paths work. Defer stream editing and extra flags until separately specified.
- [x] Make both build scripts restore/build the full solution before
  `dotnet test ... --no-build`; fail immediately on a failed phase.
- [x] Run Release build/tests on all targets, then a real-terminal startup/exit
  smoke test. Commit the executable foundation.

**Acceptance:** a runnable `dedit` shell, not an editor-shaped library package;
no working-editor claim yet.

**D1002 evidence:** `build.sh` ran restore, Release build and 17 tests per target
on Linux with SDK 10.0.401. A sized xterm pseudo-terminal displayed Untitled,
then Ctrl+Q exited with code 0 and restored the alternate screen/input modes.
`build.cmd` is written for Windows but has not been exercised here.

## D1003 — One-document editing foundation

**Current slice:** The no-path Untitled shell binds one Widgets document/view,
supports typed Unicode and paste input with undo/redo, and renders in a bounded
viewport. Integration tests exercise checkpoint identity and a tiny viewport.
The remaining checklist below still gates the full D1003 milestone.
The second tranche adds Unicode/mixed-EOL movement, four-space indentation,
column persistence, long-line resize/wrap, a repeatable benchmark, and a
million-unit allocation check. It depends on the Widgets 1.3.1 cache fix in
[Widgets PR #7](https://github.com/uniblab/Icod.Widgets/pull/7); local package
tests are green, but an independent CI restore awaits its publication.

**Files:** document/view-state types, `EditorShell.cs`, document/interaction tests;
reusable editor implementation remains in Widgets.

- [ ] Pin failing tests for grapheme movement, selection replacement, preferred
  column, CRLF boundaries, tabs, combining marks, wide characters and emoji sequences.
- [ ] Bind one Widgets document and editor view; preserve original text separately
  from display projection. Test undo/redo for typing, paste and multi-line indentation.
- [ ] Test saved-checkpoint identity through undo/redo and history eviction; include
  save-affecting metadata in dirty-state policy.
- [ ] Test wrapped/unwrapped scrolling, hit testing and caret visibility across resize,
  including a very long logical line and a viewport smaller than surrounding chrome.
- [ ] Benchmark candidate document storage/layout approaches with many-line,
  single-line and Unicode-heavy fixtures. Record latency, allocations, memory and
  cancellation baselines; agree numerical release budgets before expanding features.
- [ ] Run all tests and an interactive edit/undo/resize exercise; commit this slice.

**Acceptance:** correct responsive editing without per-keystroke whole-file copying,
full-file pad allocation, or unmeasured performance claims.

## D1004 — Commands, focus and forms

**Current slice:** View → Go to File routes both a menu mnemonic and a direct
Ctrl+P shortcut through one Widgets action. The live command focuses the tab
strip. Nested Escape and modal shortcut isolation have integration coverage;
the rest of this milestone's menus and forms are still open.

**Files:** `CommandRouter.cs`, `EditorShell.cs`, interaction tests.

- [ ] Add failing tests for Alt+V then plain F versus Ctrl+P, menu Escape, disabled
  commands, focused-field copy/undo, and modal blocking of background commands.
- [ ] Integrate menus and dynamic state with common command identities; bindings,
  menu activation and toolbar/status actions must not implement divergent commands.
- [ ] Add explicit focus cycling; test editor indentation versus dialog traversal
  and keyboard access to every essential action.
- [ ] Test paste containing tabs/newlines/control-key-like text as one undoable edit,
  without executing commands; test key releases do not repeat an action.
- [ ] Exercise drag selection outside bounds and cancellation on focus loss/modal
  open; wire elapsed-time autoscroll through the application loop.
- [ ] Add bordered/captioned dialog composition and assert Escape reports Cancel
  even when no Cancel button is displayed. Verify with deterministic interaction
  tests and a keyboard-only smoke test; commit.

**Acceptance:** active control receives intended input, with no accidental edits
behind a menu/dialog and no keyboard trap in the editor.

## D1005 — Files and tabs

**Files:** `DocumentManager.cs`, `EditorDocument.cs`, `DocumentFileService.cs`,
`TextFileCodec.cs`, tab bindings, filesystem/document tests.

- [ ] Test decoding/round-trip fixtures for UTF-8 with/without BOM, UTF-16 LE/BE,
  mixed CRLF/LF/CR, empty files, final newline and invalid byte sequences.
  Unsupported/lossy decoding must require an explicit decision, never silent replacement.
- [ ] Implement the accepted encoding/path policies; define behavior for missing,
  inaccessible, read-only and binary-looking inputs without discarding other open tabs.
- [ ] Test duplicate paths, case distinctions, aliases, and identical basenames in
  different directories. Opening an already-open identity activates its tab.
- [ ] Add stable document IDs and tab bindings with independent undo/caret/selection/
  scroll state; test overflow and active-tab fallback after closing.
- [ ] Test cancellation/out-of-order completion of opens, switching during load, and
  closing a loading tab. Late completion may not overwrite or resurrect a document.
- [ ] Run filesystem and interaction tests, then open multiple real files and switch
  repeatedly; commit.

**Acceptance:** tabs represent actual independent documents, not interchangeable
views over one shared text buffer.

## D1006 — Safe saves and lifecycle

**Files:** file service, `CloseCoordinator.cs`, document manager, failure-injection tests.

- [ ] Write failing tests for dirty Save/Save As, overwrite rejection, read-only
  destinations, disk/write/rename failures, and a target changed since it was read.
- [ ] Implement snapshot serialization and same-directory temporary writes followed
  by the platform-appropriate recoverable replacement strategy. Define permissions,
  symlink/hard-link policy, conflict recheck and temporary-file cleanup explicitly.
  Do not promise cross-platform atomicity/durability beyond verified filesystem behavior.
- [ ] Test edits during save: completion updates the saved checkpoint for the
  snapshot actually written, never falsely clears the newer buffer.
- [ ] Implement close/save/discard/cancel as a state machine. Test Escape and save
  failure retain tabs, Save As cancellation changes neither identity nor contents,
  and saving over another open document's path follows the accepted collision policy.
- [ ] Add Save All, Close All and Exit sequencing. Test cancellation at the second
  dirty tab, partial saves, late completion, and attempts to quit during active work.
- [ ] Detect external changes on relevant activation/save boundaries; offer explicit
  reload/keep/conflict choices without silent reload of dirty buffers.
- [ ] Run injected-failure and real-filesystem tests. Inspect both disk bytes and
  in-memory state after failures; commit.

**Acceptance:** every destructive transition has a tested cancel/error path.
Crash recovery remains deferred; robust saving is not a claim of crash-proof editing.

## D1007 — Search, navigation and file-format commands

**Files:** `SearchController.cs`, shell forms, codec/settings tests.

- [ ] Pin literal/regex, case/whole-word, next/previous, wrap, no-match, zero-length
  match, invalid-pattern and replacement-escaping behavior in tests.
- [ ] Add revision-aware search with cancellation and regex time limits. Test a
  pathological expression and stale matches after editing or tab switching.
- [ ] Implement replace current/all as defined undo transactions; test selection
  boundaries and replace-all returning exactly to the original via undo.
- [ ] Add Go to file/tab and validated Go to line:column using the documented
  column coordinate system, including out-of-range inputs and wrapped lines.
- [ ] Add status-driven encoding/BOM/EOL/indentation/wrap/overwrite controls.
  Distinguish reopening bytes under a different encoding from converting the
  current document; guard against losing dirty text or unrepresentable characters.
- [ ] Add validated versioned preferences with safe defaults after malformed settings;
  keep preferences separate from document saving and optional future recovery.
- [ ] Run search/format/interaction tests and a keyboard-only editing workflow; commit.

**Acceptance:** search stays responsive and cannot apply to the wrong revision;
format changes are explicit and reflected in save/dirty behavior.

## D1008 — Qualification and hardening

**Files:** regression tests, benchmark fixtures, `docs/Qualification.md`.

- [ ] Test safe rendering of escape/control characters in text, paths and errors;
  no fixture may inject terminal commands. Verify copy/save preserve original data.
- [ ] Test tiny/resized terminals, tab overflow, unavailable clipboard, cursor-style
  fallback, ambiguous modifier keys, bracketed paste, focus loss, and disposal.
- [ ] Verify internal clipboard/terminal-delivered paste remain usable without host
  clipboard queries; bound asynchronous reads and ignore stale completion.
- [ ] Repeat the D1003 performance fixtures against the agreed budgets, including
  large paste, long-line editing, repeated undo, regex cancellation and resizing.
- [ ] Run automated checks on available Windows/Linux/macOS runners; distinguish
  headless success from live-terminal qualification.
- [ ] Record real-terminal smoke evidence for open/edit/select/copy/find/replace/
  multi-tab/save/conflict/cancel/quit and terminal restoration.
  Explicitly mark unavailable platforms unverified and obtain a scoped waiver
  before any release that lacks those checks.
- [ ] Review all data-loss and terminal-restoration failure paths before declaring
  the release candidate ready; commit evidence tied to its exact revision.

**Acceptance:** reported support reflects evidence, not assumptions or inherited
qualification from the libraries.

## D1009 — Package, document and release

**Files:** delivery decision, workflows/build scripts, README, changelog, notices.

- [ ] Agree framework-dependent apphost versus NuGet .NET tool and any optional
  RID-specific/self-contained artifacts. Ensure every chosen installation exposes
  `dedit`; test clean install, launch, upgrade and removal.
- [ ] Inspect the applicable existing executable/tool workflow template and adapt
  it here. Configure no publication until package identity, credentials/trusted
  publishing and owner-approved tag triggers are confirmed.
- [ ] Verify package contents, GPL/dependency notices, source correspondence,
  version alignment, dependencies, entry point and portable paths.
- [ ] Write durable README installation and runnable first-use instructions,
  shortcut/terminal caveats and license information. Use full GitHub URLs for
  documentation not shipped in a package. Keep qualification evidence separate.
- [ ] Document implemented behavior in CHANGELOG; do not list roadmap features
  as shipped. Prepare repository/package description and keywords, with at most
  20 GitHub topics; repository-setting changes require explicit authorization.
- [ ] Execute the final clean build/test/package/install sequence and record results.
  Present the PR and remaining caveats to the owner; do not merge, tag or publish.

**Acceptance:** reproducible installable application, accurate documentation and
an owner-reviewed release checklist.

## Verification commands once projects exist

These commands are future acceptance checks, not results from the planning PR:

```text
dotnet restore Icod.Dedit.sln
dotnet build Icod.Dedit.sln -c Release --no-restore
dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Release --no-build
dotnet run --project Icod.Dedit.csproj -c Release -f net10.0 -- --help
dotnet run --project Icod.Dedit.csproj -c Release -f net10.0
```

Run framework-specific smoke tests for each claimed runtime. Once distribution is
chosen, add exact publish/pack and clean-install commands to the delivery decision.
Each implementation task follows failing test → verified failure → minimal code →
passing targeted/full tests → a small reviewable commit.

## Reference material

- [Microsoft Edit v1.2.1 source](https://github.com/microsoft/edit/tree/v1.2.1)
- [Microsoft Edit v1.2.1 release](https://github.com/microsoft/edit/releases/tag/v1.2.1)
- [Microsoft's Edit introduction](https://devblogs.microsoft.com/commandline/edit-is-now-open-source/)
- [Notepad tabs announcement](https://blogs.windows.com/windows-insider/2023/01/19/tabs-in-notepad-begins-rolling-out-to-windows-insiders/)
- [Notepad session-state announcement](https://blogs.windows.com/windows-insider/2023/08/31/new-updates-for-snipping-tool-and-notepad-for-windows-insiders/)
- [Icod.Widgets](https://github.com/uniblab/Icod.Widgets)
- [Icod.DCurses](https://github.com/uniblab/Icod.DCurses)
- [Icod.Terminal](https://github.com/uniblab/Icod.Terminal)

## Immediate next step

Review the D1001 behavior matrix and decision records, especially literal CLI
paths, cross-platform LF defaults, supported encodings, file identity and
grapheme columns. Once those policies are accepted, implement D1002's runnable
EXE foundation against released Widgets 1.3.0 and DCurses 2.5.0. This PR does
not change either library or authorize merge, tag or publication.
