# Decision 0001: file identity and tab lifetime

Status: proposed for D1001 owner review.

An open document has a generated stable ID independent of its path or visible
tab label. Normalize a supplied path with `Path.GetFullPath` against the
application's initial working directory. For an existing symlink, resolve its
final target where the managed API can do so and retain the user-visible path
separately. Compare normalized identities with `OrdinalIgnoreCase` on Windows
and `Ordinal` on Unix. Do not infer that two differently named hard links are
the same file: portable managed APIs do not supply a reliable cross-platform
file identity. A duplicate recognized identity activates its existing tab.

Two different paths may share a basename. Display the shortest distinguishable
parent suffix in their tab labels, while stable IDs control actions. Untitled
documents have unique increasing labels. Save As refuses a target already owned
by another open tab, leaving both tabs unchanged and offering to activate the
owner. Check physical aliases and external modifications again immediately
before replacement; no comparison done only at open time is sufficient. Save
through a symlink writes to the resolved target without replacing the link;
if resolution changes, treat that as a conflict. Replacing one hard-link alias
updates only that directory entry, potentially leaving other aliases with the
old bytes; do not promise preservation of hard-link relationships.

Closing the last ordinary tab leaves a zero-document workspace; a no-path
startup still creates one empty Untitled document. Exit remains a separate
command.
Closing a loading tab invalidates that operation generation; its late result
cannot recreate it. A cancelled close, Escape, failed save or denied overwrite
retains the same ID, text and view state. `Close All`/Exit may have already saved
earlier tabs when a later one cancels; only the remaining close operations stop.

CLI arguments are literal paths for the first release. Unlike Microsoft Edit's
`FILE[:LINE[:COLUMN]]` syntax, a colon and trailing digits are part of a legal
Unix filename and are never silently stripped. Navigation after opening uses
Go to Line. `--` ends option parsing; quoted spaces and Unicode paths are
handled as ordinary arguments.

Acceptance: duplicate and alias activation, same-basename labels, Windows/Unix
case behavior, Save As collision, close-last, cancellation and stale-load tests
in D1002/D1005/D1006. Symlink and hard-link behavior gets filesystem tests on
each available OS; inaccessible resolution falls back to the normalized path.
