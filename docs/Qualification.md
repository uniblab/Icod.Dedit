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
  before a release claim. The shell currently does not open or edit files.
