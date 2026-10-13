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
