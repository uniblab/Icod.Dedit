# Dedit Line-Number Gutter Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebind `dedit` to the published Widgets line-number API and enable an `edit.exe`-like numbered, current-line-aware editor without adding application-specific projection logic.

**Architecture:** Widgets owns gutter geometry, rendering, theming, and pointer semantics. `EditorShell` opts every `dedit` editor into line numbers and the current-line band, while `DeditTheme` retains application selection policy and consumes the Widgets gutter defaults. Packaging pins and verifies the exact published dependency.

**Tech Stack:** C# 13; .NET 8, 9, and 10; published Icod.Widgets 1.7.0; Icod.DCurses 2.5.2 transitively; xUnit; .NET tool packaging.

**Spec:** `https://github.com/uniblab/Icod.Widgets/blob/feature/1.7.0-editor-current-line/docs/superpowers/specs/2026-10-17-editor-line-number-gutter-design.md`

## Global Constraints

- Do not begin until Icod.Widgets `1.7.0` containing `ShowLineNumbers`, `HighlightCurrentLine`, `EditorGutter`, and `EditorGutterCurrentLine` is public on NuGet.
- Keep the `dedit` `Version` and `PackageVersion` at `1.0.0` and its assembly identity unchanged.
- Enable line numbers and current-line highlighting for every editor; add no menu toggle or persisted preference.
- Keep the gutter decorative; do not implement line selection or duplicate Widgets projection logic.
- Keep the README concise and the complete release/dependency history in `CHANGELOG.md` and `docs/releases/1.0.0.md`.
- Preserve exact-package installation, launch, same-version update, and uninstall verification.
- Do not merge or publish.

## Review Focus

- A tiny `dedit` viewport must suppress the gutter and still render/edit text; Task 2 extends the existing tiny-viewport test.
- Gutter clicks must not change caret or selection even though the editor may receive focus; Task 2 adds the application-level pointer regression.
- Current-line, selection, and gutter styles must remain visually distinct on color terminals and usable on monochrome terminals; Task 2 extends presentation tests.
- Wrapped logical lines must show one number and preserve current-line styling across continuation rows; Task 2 adds the wrapped application test.
- The packed tool manifest must require exactly Icod.Widgets `1.7.0`, not a stale 1.6.x package; Tasks 1 and 3 update and exercise package verification.

## File Structure

- Modify `Icod.Dedit.csproj` and `packaging/VerifyDeditTool.ps1` for the exact published Widgets dependency.
- Create `tests/Icod.Dedit.Tests/DeditReleaseIdentityTests.cs` for source/package metadata assertions.
- Modify `src/Presentation/EditorShell.cs` to enable both editor presentation options.
- Modify editor/presentation tests for rendered behavior and input regression coverage.
- Modify concise product documentation, full history, curated notes, and qualification evidence.

---

### Task 1: Published Widgets dependency gate

**Files:**
- Create: `tests/Icod.Dedit.Tests/DeditReleaseIdentityTests.cs`
- Modify: `Icod.Dedit.csproj`
- Modify: `packaging/VerifyDeditTool.ps1`
- Modify: `docs/Library-Contracts.md`
- Modify: `docs/releases/1.0.0.md`

**Interfaces:**
- Consumes: published `Icod.Widgets` package version `1.7.0`.
- Produces: exact build and package dependency on Icod.Widgets `1.7.0` while retaining `dedit` `1.0.0`.

- [ ] **Step 1: Confirm the publication gate.** Run `dotnet nuget search Icod.Widgets --take 20` or restore a temporary package reference and confirm version `1.7.0` is available from the configured NuGet source. Stop and request owner intervention if it is unavailable.
- [ ] **Step 2: Write failing release-identity tests.** Add `CandidateUsesPublishedWidgets170` and `ToolVerifierRequiresWidgets170`. Assert `Icod.Dedit.csproj` pins `1.7.0`, retains `Version`/`PackageVersion` `1.0.0`, and the verifier requires `Icod.Widgets/1.7.0` without a 1.6.x fallback.
- [ ] **Step 3: Run the focused tests and verify red.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~DeditReleaseIdentityTests`; expect assertions showing the current 1.6.2 dependency.
- [ ] **Step 4: Rebind source and packaging.** Change the package reference and dependency-manifest assertion to exactly `1.7.0`; update library-contract and curated-note dependency statements without changing `dedit` version metadata.
- [ ] **Step 5: Restore and run focused tests.** Run `dotnet restore Icod.Dedit.sln` and the Step 3 test command; expect restore from public NuGet and all selected tests to pass.
- [ ] **Step 6: Commit.** Commit dependency, verifier, tests, and dependency docs with `build: rebind dedit to Widgets 1.7.0`.

### Task 2: Enable and qualify the `dedit` editor presentation

**Files:**
- Modify: `src/Presentation/EditorShell.cs`
- Modify: `tests/Icod.Dedit.Tests/EditorShellTests.cs`
- Modify: `tests/Icod.Dedit.Tests/DeditPresentationTests.cs`

**Interfaces:**
- Consumes: Widgets `WidgetEditorOptions.ShowLineNumbers`, `HighlightCurrentLine`, and the two gutter theme roles.
- Produces: every `EditorShell.Editor` constructed with `ShowLineNumbers = true` and `HighlightCurrentLine = true`; no new `dedit` public API.

- [ ] **Step 1: Write failing application-level presentation tests.** Add `EditorShowsLineNumbersAndTracksCurrentLogicalLine`, `WrappedEditorShowsOneNumberAndCurrentStyleAcrossContinuations`, `DecorativeGutterClickLeavesSelectionUnchanged`, and extend `TinyViewportKeepsDocumentWithoutAllocatingScreenForText` to assert that text retains its final column when the gutter cannot fit. In presentation tests, assert color-capable themes distinguish `EditorGutter` from `EditorGutterCurrentLine` and preserve explicit selection precedence; assert monochrome creation uses only supported attributes.
- [ ] **Step 2: Run focused tests and verify red.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~EditorShellTests|FullyQualifiedName~DeditPresentationTests"`; expect missing line labels/current-line band while existing tests stay green.
- [ ] **Step 3: Enable both editor options.** Set `ShowLineNumbers = true` and `HighlightCurrentLine = true` in the existing `WidgetEditorOptions` initializer in `EditorShell`; do not add a `dedit` renderer, gutter widget, menu command, or preference.
- [ ] **Step 4: Preserve theme ownership.** Leave `DeditTheme`'s explicit selection override unchanged and consume Widgets' capability-aware gutter styles directly; the presentation tests must prove the resulting roles remain distinct without terminal-brand checks or application-specific rendering.
- [ ] **Step 5: Run focused tests and verify green.** Run the Step 2 command; expect all selected tests to pass.
- [ ] **Step 6: Run the complete unit suite.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Release -f net10.0`; expect zero failures.
- [ ] **Step 7: Commit.** Commit source and tests with `feat: show line numbers in dedit`.

### Task 3: Product documentation, package validation, and live qualification

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/releases/1.0.0.md`
- Modify: `docs/Qualification.md`
- Modify: `tests/Icod.Dedit.Tests/DeditReleaseIdentityTests.cs`

**Interfaces:**
- Consumes: Tasks 1-2's published dependency and enabled presentation.
- Produces: an unmerged `dedit` 1.0.0 candidate with curated user documentation and exact package evidence.

- [ ] **Step 1: Extend failing documentation assertions.** Require the curated notes to name Widgets `1.7.0`, line numbers, themed current-line indication, decorative gutter clicks, wrapped-line behavior, and tiny-view suppression. Require the README to describe the visible numbered editor in one concise passage without release history.
- [ ] **Step 2: Run focused release tests and verify red.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~DeditReleaseIdentityTests`; expect missing documentation assertions.
- [ ] **Step 3: Update documentation.** Add the concise README presentation description; add full dependency/feature history to `CHANGELOG.md`; expand curated `1.0.0` notes with behavior and limitations. Do not describe gutter selection, runtime toggling, or configurable numbering because they are out of scope.
- [ ] **Step 4: Run focused release tests and verify green.** Run the Step 2 command; expect all selected tests to pass.
- [ ] **Step 5: Run the full local pipeline.** Run `build.cmd` on Windows or `./build.sh` on Unix-like systems; expect clean restore/build/test/pack/validate, exactly one `Icod.Dedit.1.0.0.nupkg`, manifest dependency `Icod.Widgets/1.7.0`, and successful install/launch/same-version update/uninstall.
- [ ] **Step 6: Run the live editor.** Run `dotnet run --project Icod.Dedit.csproj -c Release -f net10.0 -- README.md`; verify fixed extreme-left line numbers, blank wrapped continuations, current-line band and number, dominant selection, accurate text clicks, decorative gutter clicks, horizontal scrolling, word-wrap toggling, and narrow resize behavior.
- [ ] **Step 7: Push completed commits to Dedit PR #1 and wait for CI.** Require Linux, Windows, and macOS jobs to pass across .NET 8/9/10, including exact package verification and tool lifecycle.
- [ ] **Step 8: Record exact qualification evidence.** Add the final commit/run URL, per-framework test totals, zero-warning build result, package identity, and tool lifecycle outcome to `docs/Qualification.md`; commit as `docs: record line-number gutter qualification`, push, and require the documentation-only CI run to pass. Do not merge or publish.
