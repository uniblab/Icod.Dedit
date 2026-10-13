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
  before a release claim. Supplied paths are not loaded or editable.

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
