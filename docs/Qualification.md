# Qualification record

This record contains historical checkpoints and current development evidence.
Earlier sections describe their named revisions; they do not qualify the
current head or certify a 1.0 release.

## Current development head — 2026-10-20

| Revision and run | Scope | Result |
| --- | --- | --- |
| [Dedit `ea688f9` / 39502956271](https://github.com/uniblab/Icod.Dedit/actions/runs/39502956271) | Published Widgets 1.9.0 rebind; opt-in auto-hidden vertical editor scrollbar; Dedit integration regressions for overflow visibility and Ctrl+Down displayed-row scrolling; concise README, curated notes and exact dependency verification | Metadata plus Linux, Windows and macOS jobs passed. Each OS built successfully, passed 250 tests per .NET 8/9/10 target, verified exactly one `Icod.Dedit.1.0.0.nupkg` whose three payloads require `Icod.Widgets/1.9.0`, and installed, launched, same-version updated and removed it. Linux reported zero build warnings/errors; Windows also repacked after seeding a stale 0.1.0 package and repeated exact package and tool-lifecycle validation successfully. |
| [Dedit `916cb3e` / 39187729068](https://github.com/uniblab/Icod.Dedit/actions/runs/39187729068) | True zero-document workspace after final Close/Close All; hidden and unfocusable empty tab strip; blank ordinary status and caret; disabled document commands; New/Open recovery; Exit from empty; aligned acceptance and release documentation | Metadata plus Linux, Windows and macOS jobs passed. Each OS built successfully, passed 248 tests per .NET 8/9/10 target, verified exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it. Linux reported zero warnings/errors; Windows also repacked after seeding a stale 0.1.0 package and repeated exact package and tool-lifecycle validation successfully. |
| [Dedit `39d062f` / 39183759756](https://github.com/uniblab/Icod.Dedit/actions/runs/39183759756) | Dedit-owned compact, centered and resize-clamped About bounds; shared unchanged-layout guard for About and dirty-close pointer clicks; curated release documentation | Metadata plus Linux, Windows and macOS jobs passed. Each OS built with zero warnings/errors, passed 242 tests per .NET 8/9/10 target, verified exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it. Windows also repacked after seeding a stale 0.1.0 package and repeated exact package and tool-lifecycle validation successfully. |
| [Dedit `cd0573e` / 39177582562](https://github.com/uniblab/Icod.Dedit/actions/runs/39177582562) | Dedit-owned compact, centered and resize-clamped dirty-close bounds; redraw-safe dialog clicks; preserved generic Widgets 1.8.0 modal/layout contract; curated release and library-contract documentation | Metadata plus Linux, Windows and macOS jobs passed. Each OS built with zero warnings/errors, passed 238 tests per .NET 8/9/10 target, verified exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it. Windows also repacked after seeding a stale 0.1.0 package and repeated exact package and tool-lifecycle validation successfully. |
| [Dedit `569d49b` / 39175246987](https://github.com/uniblab/Icod.Dedit/actions/runs/39175246987) | Published Widgets 1.8.0 rebind; generic dirty-close Yes / Don't Save / Cancel actions; capability-aware dirty-only warning styles; obsolete YesNo migration; release documentation and exact package lifecycle | All Linux, Windows and macOS jobs passed. Each OS built with zero warnings/errors, passed 234 tests per .NET 8/9/10 target, verified exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it; Windows also passed stale-package cleanup. Local serial Debug/Staging validation produced the same counts and confirmed all three payload manifests contain `Icod.Widgets/1.8.0`. The stripped-down local SDK's parallel workload resolver silently failed solution Clean/Pack, while identical `-m:1 --disable-build-servers` commands passed. |
| [Dedit `09193ec` / 39164365676](https://github.com/uniblab/Icod.Dedit/actions/runs/39164365676) | Published Widgets 1.7.2 rebind; identical pointer Paste over the active copied selection completes the selection and a following pointer Paste inserts again; aligned package and release contracts; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 216 tests per .NET 8/9/10 target. Run 39163981049 retained all 215 prior tests and failed only the new identical-paste selection-completion regression on each target against Widgets 1.7.1. The package verifier required Widgets 1.7.2, checked exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it. Windows also passed stale-package cleanup. |
| [Dedit `96345ed` / 39071905635](https://github.com/uniblab/Icod.Dedit/actions/runs/39071905635) | Preserve menu-item pointer clicks across Dedit's redraw-time layout pass; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 215 tests per .NET 8/9/10 target. Run 39071653508 retained all 214 prior tests and failed only the new redraw-layout click regression on each target before the correction. The package verifier checked exactly one `Icod.Dedit.1.0.0.nupkg`, required Widgets 1.7.1, and installed, launched, same-version updated and removed it. Windows also passed stale-package cleanup. |
| [Dedit `7e7fae8` / 39070201419](https://github.com/uniblab/Icod.Dedit/actions/runs/39070201419) | Published Widgets 1.7.1 rebind; wrapped visual-row movement, preferred-column retention, logical line/character status regression and aligned released-library contract; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 215 tests per .NET 8/9/10 target. The new behavior and exact dependency/documentation contracts failed against Widgets 1.7.0 in run 39069163080 and passed after the rebind. Final review then found a stale library-contract reference; run 39070053193 retained all 214 prior tests and failed only its new assertion before correction. The package verifier required Widgets 1.7.1, checked exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it. Windows also passed stale-package cleanup. |
| [Dedit `eb59ef9` / 39064571852](https://github.com/uniblab/Icod.Dedit/actions/runs/39064571852) | Final-review correction preserving text-selection drags across ordinary redraws; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 213 tests per .NET 8/9/10 target. The new regression failed before the correction in run 39064418224 and passed afterward. Exact package verification, install, launch, same-version update and removal passed; Windows also passed stale-package cleanup. |
| [Dedit `d2bd291` / 39063576613](https://github.com/uniblab/Icod.Dedit/actions/runs/39063576613) | Published Widgets 1.7.0 rebind; fixed line-number gutter, themed current-line band, wrapped-row, decorative-click and narrow-viewport regressions; curated documentation; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 212 tests per .NET 8/9/10 target. The package verifier required Widgets 1.7.0, checked exactly one `Icod.Dedit.1.0.0.nupkg`, and installed, launched, same-version updated and removed it. Windows also passed the stale-package cleanup regression. |
| [Dedit `35779d4` / 38983306761](https://github.com/uniblab/Icod.Dedit/actions/runs/38983306761) | Redraw-safe menu clicks, physical bar/block caret, explicit color selection with monochrome fallback and `SEL` status; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 205 tests per .NET 8/9/10 target. The package verifier checked exactly one 1.0.0 package and installed, launched, same-version updated and removed it. Windows also passed the stale-package cleanup regression. |
| [Dedit `68c15ec` / 38981129239](https://github.com/uniblab/Icod.Dedit/actions/runs/38981129239) | Widgets 1.6.2 rebind; Dedit menu click, nested overlay, Unicode border, About-button and button-motion protocol regressions; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Each OS built with zero warnings/errors and passed 200 tests per .NET 8/9/10 target. The package verifier required Widgets 1.6.2, verified exactly one 1.0.0 package, and installed, launched, same-version updated and removed it. Windows also passed the stale-package cleanup regression. |
| [Dedit `119c795` / 38841351117](https://github.com/uniblab/Icod.Dedit/actions/runs/38841351117) | Widgets 1.6.1 rebind; Staging restore/build/test/pack and exact local tool lifecycle on Linux, Windows and macOS | All jobs passed. Linux logged 195 tests per .NET 8/9/10 target, and the packaged dependency manifest required Widgets 1.6.1. Each OS installed, launched, same-version updated and removed the exact 1.0.0 tool package. |
| [Dedit `b9904c9` / 38592772413](https://github.com/uniblab/Icod.Dedit/actions/runs/38592772413) | Staging restore/build/test/pack/local tool lifecycle on Linux, Windows and macOS; Windows local-pack regression seeded an old 0.1.0 package | All jobs passed. The regression repacked 1.0.0, verified exactly one package, and installed, launched, updated and removed the tool. |
| [Dedit `7694cb1` / 38589379098](https://github.com/uniblab/Icod.Dedit/actions/runs/38589379098) | Test-only handling of Windows symbolic-link privilege denial; Staging restore/build/test/pack and local tool lifecycle on Linux, Windows and macOS | All jobs passed. Linux ran 195 tests per .NET 8/9/10 target with zero skips and zero build warnings/errors. Windows CI retained real symbolic-link coverage. |
| [Dedit `c7cd528` / 38586803805](https://github.com/uniblab/Icod.Dedit/actions/runs/38586803805) | 1.0.0 candidate using Widgets 1.5.0; Staging restore/build/test/pack, package metadata and local tool lifecycle on Linux, Windows and macOS | All jobs passed. Linux logged 195 tests per .NET 8/9/10 target, zero build warnings/errors, exact package license/runtime dependency/content checks, and install, launch, same-version update and removal. |
| [Dedit `cfdf78a` / 38585208213](https://github.com/uniblab/Icod.Dedit/actions/runs/38585208213) | Widgets 1.5.0 linewise clipboard integration; Staging restore/build/test/pack/local tool lifecycle on Linux, Windows and macOS | All jobs passed. Linux logged 195 tests per .NET 8/9/10 target, zero build warnings/errors, exact 0.1.0 package validation, and local tool install, launch, same-version update and removal. |
| [Dedit `59f38f8` / 38486418817](https://github.com/uniblab/Icod.Dedit/actions/runs/38486418817) | Real hard-link filesystem test plus Staging restore/build/test/pack/local tool installation on Linux, Windows and macOS | All jobs passed. Linux logged 192 tests per .NET 8/9/10 target and zero build warnings/errors. Two hard-link names open separate tabs; saving one replaces only its directory entry and leaves the other name's original bytes. |
| [Dedit `a68de78` / 38480327720](https://github.com/uniblab/Icod.Dedit/actions/runs/38480327720) | Staging restore/build/test/pack/local tool installation on Linux, Windows and macOS, using the released Icod.Widgets 1.4.0 package | All jobs passed. Linux logged 191 tests per .NET 8/9/10 target, zero failures and zero build warnings/errors. |
| [Widgets 1.4.0](https://github.com/uniblab/Icod.Widgets/pull/9) | Per-editor newline and indentation API | Merged to main and available to the Dedit CI NuGet restore. |

The current integration uses the first loaded line terminator for future Enter,
updates the policy after explicit conversion, and offers per-tab indentation
widths. Reload retains the tab's chosen indentation width. The headless CI
package checks do not replace a current-head live-terminal exercise.

### Remaining release gates

- Widgets 1.10.0 is published and restored from NuGet in the current Dedit
  candidate. Dedit enables its standard auto-hidden vertical and horizontal
  editor scrollbars; the horizontal bar appears only in no-wrap mode. Widgets
  owns two-axis range and layout, rendering, pointer and keyboard scrolling,
  context commands, and caret/viewport synchronization. Its explicit generic
  dialog actions and per-instance styles supply
  the common keyboard/pointer mechanism for Dedit's dirty-only red warning
  prompt. Dedit owns its Yes / Don't Save / Cancel captions, Y/N/C shortcuts,
  Cancel default, and guarded save/discard policy. The dependency also carries
  identical-paste completion, wrapped visual-row navigation,
  preferred-column retention, editor gutter and current-line presentation are
  integrated.
  No-selection Copy/Cut and local linewise Paste are integrated;
  changed terminal clipboard text pastes as plain text. The dependency also
  carries corrected menu/dialog interaction, nested overlay rendering and the
  DCurses 2.5.2 / Terminal 1.29.2 input chain. Dedit requests button motion
  for selection drags without enabling unsolicited hover motion. Live terminal
  pointer and clipboard interoperability still need a current-head exercise.
- The accepted hard-link policy is exercised on all three CI filesystems.
  The final cross-process replacement race remains: fingerprint/target
  rechecks narrow the window but are not a portable atomic compare-and-swap.
- Run a keyboard-only current-head terminal exercise of open/edit/select/
  copy/find/replace/tabs/format/save/conflict/cancel/quit and restoration.
  On Windows at `eb69a72`, the owner's `build.cmd` completed clean, restore and build
  with zero warnings/errors, then stopped on five symbolic-link tests per target
  because that process lacked Windows link-creation privilege (error 1314).
  At `d65c1d4`, the owner's rerun completed Clean, Restore and Build with
  zero warnings/errors; Test reported 190 passed, five skipped and zero failed
  on each of .NET 8/9/10. Pack produced `Icod.Dedit.1.0.0.nupkg`, but Validate
  stopped because the same local `artifacts` directory retained an older
  `Icod.Dedit.0.1.0.nupkg`. Local pack now clears prior package outputs before
  validating the exact candidate. At `e06190d`, the owner's full Windows
  `build.cmd` reached Validate, verified exactly one 1.0.0 package, and
  completed install, launch, same-version update and removal from that
  package. A separate Release net10.0 `--version` returned `dedit 1.0.0`.
  On 2026-10-17, the owner confirmed the line-number gutter and word-wrap
  behavior on the then-current candidate, and reported that menu items still
  ignored pointer clicks. Run 39071653508 reproduced that remaining redraw
  defect before the correction in `96345ed`; its live menu-click retest and the
  rest of the terminal exercise remain outstanding. The owner deferred WSL
  because of checkout line endings.
- The 1.0.0 Staging package checks source-corresponding README,
  CHANGELOG and LICENSE, GPL expression, embedded Widgets 1.8.0 runtime
  dependency, three tool payloads, install/launch, same-version update and
  removal. This is the first public Dedit version, so there is no published
  older version for a cross-version upgrade test. Release-config six-runner
  validation and the tagged workflow remain unrun before owner merge/tag.
  No Dedit tag or publication has occurred.

### Owner terminal exercise on the 1.0.0 candidate

Use the current `docs/dedit-master-plan` PR head in a disposable working
directory. Record the exact commit, OS and terminal for each exercise.

1. On Windows in a VS Developer Command Prompt, run `build.cmd`, then
   `dotnet run --project Icod.Dedit.csproj -c Release -f net10.0 -- --version`.
   Expect `dedit 1.0.0`. Repeat `./build.sh` in WSL if available.
2. Launch `dotnet run --project Icod.Dedit.csproj -c Release -f net10.0`
   in an interactive terminal. Open File with Alt+F, cycle root menus with the
   arrow keys, open a nested File submenu and verify its parent remains visible.
   Click a menu command, open Help → About and verify its compact centered
   presentation, resize the terminal to confirm it recenters/clamps, then click
   OK. Drag a text selection.
   Select text, click Edit → Copy, then click Edit → Paste without moving the
   selection. Verify the unchanged text is no longer selected, then click
   Edit → Paste again and verify a second copy is inserted at the caret.
   Verify fixed line numbers at the extreme left, a themed current-line band
   and number, selection remaining visually dominant, blank number cells on
   wrapped continuation rows, decorative gutter clicks leaving selection
   unchanged, and gutter suppression after resizing too narrow for both gutter
   and text. On a logical line that wraps, move vertically through its visual
   rows and across a short row; verify the preferred visual column returns and
   the status continues to show the real logical line and character position.
   Type two lines, use Ctrl+C with no selection, move to the next line and
   Ctrl+V, then Ctrl+Z. Cut a line without a selection and undo it. Also select
   a range and verify ordinary replacement.
3. Open two disposable files/tabs. Use F6 and the tab strip, Find/Replace,
   Go to Line:Column, line-ending conversion and indentation controls.
   Save and reopen to inspect exact text and line endings. Close both tabs and
   verify the tab strip, editor, caret and ordinary status text disappear;
   Edit, View and document-dependent File commands should be disabled. Use New
   and Open in turn to verify each restores the document workspace.
4. Save a disposable file, change it from a second process, then activate its
   tab again. Verify Reload/Keep is explicit, Keep preserves the buffer,
   an original-path save refuses the conflict, and Save As to a different
   name succeeds. Cancel a dirty Close/Exit decision; then complete Exit.
5. After exit or Ctrl+Q, type a normal command in the terminal. Confirm echo,
   cursor, alternate-screen state and input modes have been restored. Report
   any unavailable or failed environment rather than treating headless CI
   as a live terminal pass.

Do not tag or publish until the owner reviews this record and the final
Release-config distribution matrix after merge.

## D1002 executable foundation

- Linux, .NET SDK 10.0.401, Release: `sh build.sh` restored and built the
  complete solution with zero warnings and errors; xUnit passed 17/17 on
  each of net8.0, net9.0 and net10.0 using runtime roll-forward locally.
- Actual net10.0 process: `--help` printed usage, `--version` printed
  `dedit 0.1.0`, and an unknown option diagnosed the name and exited 2.
- Interactive startup/exit: a sized 24x80 xterm pseudo-terminal showed
  the Untitled shell; Ctrl+Q returned 0. The session emitted the matching
  disable/restore terminal protocol and alternate-screen sequences.
- Windows `build.cmd`, macOS, and a hardware terminal remain to be checked
  before a release claim. At this D1002 checkpoint, paths were placeholders;
  D1005 below records the later file-loading work.

## D1003 in-memory editor slice

The no-path Untitled session hosts a Widgets document, view and multiline
editor. Integration tests cover a combining-mark edit, undo back to the saved
checkpoint, redo, paste, and rendering in a three-row viewport. File I/O,
multi-document editing, performance budgets and the rest of D1003 remain open.
The Release solution passed 19/19 tests on each target; the net10.0 shell
accepted text plus Ctrl+Z/Ctrl+Y and exited with Ctrl+Q in a sized xterm
pseudo-terminal.

The next D1003 source tranche pins Widgets 1.3.1, pending publication from
[Widgets PR #7](https://github.com/uniblab/Icod.Widgets/pull/7), to correct
cached-line invalidation at an insertion boundary. The candidate package was
built locally from that PR; its package-only consumer ran successfully. Dedit
tests then passed 25/25 on each target, including first-character rendering,
mixed Unicode/EOL movement, four-space indentation, preferred columns,
resize/wrap, and a million-unit viewport allocation assertion. This is local
candidate qualification, not an external NuGet restore or cross-platform test.

The repeatable `benchmarks/Icod.Dedit.Measurements` Release run on Linux with
SDK 10.0.401 measured the following (single cold run; JIT, hardware, and load
can change elapsed times):

| Fixture | UTF-16 units / lines | Load | Single edit | Cold render | Cold allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Many short CRLF lines | 1,200,000 / 100,001 | 5.359 ms | 7.083 ms | 26.258 ms | 373,768 B |
| One long line | 1,000,000 / 1 | 1.077 ms | 0.049 ms | 3.248 ms | 1,402,120 B |
| Mixed Unicode CRLF lines | 200,000 / 20,001 | 0.216 ms | 0.014 ms | 1.339 ms | 245,208 B |

The application caps projection input to 4,096 source units per update. Its
correctness test bounds a million-unit cold viewport render to 5 MB allocated
on the test thread; timings above are observations, not portable pass/fail
limits. File I/O, multi-document editing, history eviction, and real terminal
editing under this candidate still need qualification.

Further D1003 tests exercise replacement of a combining cluster as one undoable
edit, saved snapshot identity through undo/redo, and pointer hit testing after
resize over a wide glyph. The Release build passed with zero warnings or errors
and all 28 tests passed on each of net8.0, net9.0, and net10.0. A sized 24×80
xterm pseudo-terminal displayed typed
text, accepted Ctrl+Z/Ctrl+Y, and exited with Ctrl+Q (exit 0); both alternate
screen enter and exit sequences were observed. An initial unsized pseudo-terminal
attempt exited 1, so it is not counted as a successful smoke check. Hardware
terminal, Windows, and macOS observations remain outstanding.

## D1004 first menu route

The View menu now routes its Go to File mnemonic (`Alt+V`, then plain `F`)
and `Ctrl+P` through one command identity. The terminal action focuses the
tab strip. Unit interaction tests cover the distinct paths, Escape backing
out of nested menus, and an OK modal blocking that shortcut with Escape
reporting Cancel. A sized xterm pseudo-terminal with two path captions showed
the focused tab after `Ctrl+P` and restored the terminal after Ctrl+Q.
The local xterm's raw Escape+V byte sequence did not report an Alt modifier and
was interpreted as ordinary text; it is not live evidence for Alt+V support.
The remaining menu, commands, forms, and file loading milestones remain open.
Edit → Undo/Redo now dispatches through the focused Widgets edit target.
Menu availability follows actual undo/redo history, while Ctrl+Z/Ctrl+Y
remain direct editor routes. A test exercises menu Undo followed by direct
Redo and the disabled state of an empty history.
View → Word Wrap and Alt+Z share one checked action. An integration test toggles
the editor between word wrap and no wrap, confirms the menu's checked state,
and verifies the stored document text is unchanged.
Help → About now opens a captioned OK dialog with the executable version and
GPL identity. A menu integration test confirms Escape returns Cancel and the
dialog closes. Live About interaction remains to be exercised on a terminal.
The loop now sends Ctrl+Q to the terminal adapter, which suppresses quitting
while a menu or modal dialog is open. Tests cover both scopes and the normal
quit path; a sized xterm pseudo-terminal still exited 0 and restored alternate
screen state after Ctrl+Q. This does not yet provide dirty-document quit
confirmation, which remains D1006 work.
F6 and Shift+F6 now cycle focus without taking Tab away from indentation.
Tests cover forward/backward cycling, ignored function-key release, and focus
remaining inside an active modal scope.
Edit → Cut/Copy/Paste/Select All and Ctrl+X/C/V/A now target the focused editor.
Clipboard completions use a 64 KiB process-local fallback and Widgets' guarded
request identity. Tests cover a menu copy followed by direct cut/paste and undo,
an empty clipboard, a stale completion after focus changes, literal tab/newline/
control characters as one paste edit, ignored key release, and modal isolation.
Terminal clipboard transfer and no-selection linewise behavior remain open.

## D1005 text codec boundary

Strict UTF-8, UTF-8 BOM, UTF-16 LE BOM and UTF-16 BE BOM round-trip tests cover
empty input, Unicode, mixed CRLF/LF/CR and final-newline differences. Invalid
UTF-8, malformed UTF-16, BOM-less UTF-16-like bytes containing NUL and decoded
NUL content are rejected rather than replaced. The codec is not yet wired to
the tabbed UI; a bounded read service enforces size before decode.
The bounded service now resolves relative paths against the initial directory,
enforces a 32 MiB limit before and during asynchronous reads, and records a
SHA-256 fingerprint of the original bytes. Filesystem tests cover an actual
UTF-16 BE file, missing/invalid/oversize input and cancellation. It is not yet
connected to the tabbed UI.
The terminal shell now connects these snapshots to independent document/view
instances and switches on stable keys. Headless tests retain caret, undo and
dirty state across switching, deduplicate a normalized path, distinguish equal
basenames and preserve tab-strip focus for repeated arrows. A sized xterm
pseudo-terminal opened two real UTF-8 files, displayed the first, routed Ctrl+P
to the strip and displayed the second with application-cursor Right (`ESC O C`).
The shell restored its alternate screen on Ctrl+Q. An earlier probe used
normal-cursor `ESC [ C` while this session had application-cursor mode enabled;
that probe did not exercise the intended arrow input.

## D1006 initial exit guard

Ctrl+Q checks every tab for modified text. A dirty workspace shows a captioned
Yes/No discard prompt with No selected by default; Escape returns Cancel and
keeps every document. Headless integration tests cover clean exit, active and
inactive modified documents, repeated quit while modal, cancellation and
explicit Yes. Save and close workflows are not implemented yet.
