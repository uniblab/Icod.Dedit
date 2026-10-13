# Qualification record

This record describes checks actually run on the current branch. It is not a
cross-platform release certification.

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
