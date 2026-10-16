# Changelog

All notable user-visible changes in this development branch are recorded here.
The entries under Unreleased describe implemented behavior. They are not a
1.0.0 release announcement.

## Unreleased — 1.0.0 candidate

### Application and editing

- Feed idle elapsed-time updates to Widgets for drag autoscroll, repainting
  after a visible update instead of redrawing every idle tick.
- Added the `dedit` console executable and .NET tool payloads for .NET 8, 9
  and 10, with a no-path Untitled document and multiple path arguments.
- Added independent editable tabs with stable keys, disambiguated duplicate
  basenames, per-tab caret and undo history, a keyboard-accessible tab strip,
  and an overflow-aware tab view.
- Added Unicode text editing with grapheme-aware movement, selection, typed
  input and bracketed paste, mixed line-ending preservation, four-space
  indentation, undo/redo, and optional word wrap per view.
- Added File → New/Open/Save/Save As/Save All/Close/Close All/Exit and Edit →
  Undo/Redo/Cut/Copy/Paste/Select All. Open accepts multiple files in one
  picker. The 64 KiB process-local clipboard also uses DCurses terminal clipboard I/O
  on explicit copy/paste with a bounded read and local fallback. With no
  selection, Copy/Cut transfers the current logical line, retaining its exact
  terminator; local linewise Paste inserts at the destination line start. A
  different terminal clipboard value is plain text.
- Added Find and Replace dialogs with literal or .NET regex search, invariant
  case matching, whole-word matching, forward/backward navigation and wrap.
  F3 and Shift+F3 repeat within the current tab. Replace All makes one
  undoable edit; search results are tied to a document revision, with finite
  regex and overall work limits.
- Added Go to File/tab, a validated one-based Go to Line:Column prompt using
  Unicode grapheme columns, F6 focus cycling, Word Wrap and About.
- Added explicit UTF-8/UTF-8 BOM/UTF-16 LE/UTF-16 BE selection that contributes
  to unsaved state and checkpoints only the encoding written. LF/CRLF/CR
  normalization preserves final terminators and is one undoable edit. Opening
  a file uses its first line ending for future Enter keys; explicit conversion
  updates that choice even in a document with no existing terminators.
- Added per-tab 2-, 4- and 8-space indentation controls, backed by the
  mutable editor policy in Icod.Widgets 1.4.0. Switching width affects future
  indentation without rewriting text or marking a clean document dirty.

### Files and safety

- Escape terminal control characters in command-line and startup diagnostics.
- Resolve file and parent-directory symlinks consistently for duplicate tabs,
  Save As collisions and save-time retarget checks. New-file saves keep their
  temporary file in the resolved directory and recheck cancellation immediately
  before replacement.
- Added strict UTF-8 and BOM-marked UTF-16 LE/BE decoding, exact mixed
  line-ending and final-terminator round trips, a 32 MiB open/save limit,
  and explicit diagnostics for missing, invalid or oversized files.
- Added snapshot saves through same-directory temporary files, original-byte
  fingerprints and path-target checks before replacement. Read-only,
  externally changed and another tab's targets are refused; failed writes
  clean up temporary files. Existing-target Save As requires an extra
  confirmation with No selected by default.
- Added per-tab Save / Don't Save / Cancel decisions for dirty Close All
  and Exit, with Save All processing tabs in order. Cancel leaves later tabs
  intact and preserves earlier successful saves.
- Added explicit Reload/Keep decisions when an activated file has changed
  outside the editor or a direct save finds a conflict. Reload confirms
  discarding dirty edits; Keep retains the buffer and the original-path
  save conflict remains in force.

- Added an optional versioned, bounded startup preferences file for tab width,
  indentation width, wrap and new-document encoding. Invalid settings use safe
  defaults without altering document saves.

### Build and validation

- Updated the application dependency to Icod.Widgets 1.6.1. This retains the
  1.5.0 linewise clipboard integration while incorporating corrected menu and
  dialog interaction plus the DCurses 2.5.1 / Terminal 1.29.1 input chain.
- Added local cmd/sh/PowerShell build and package scripts, pull-request
  validation across Linux, Windows and macOS, and guarded tag-triggered
  package/archive release workflows. No release has been published.
