# Dirty-Close Dialog Integration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebind Icod.Dedit 1.0.0 to published Icod.Widgets 1.8.0 and present every dirty-document close decision as a red, caption-customized Yes / Don't Save / Cancel message dialog whose pointer and keyboard paths share identical application behavior.

**Architecture:** Icod.Widgets owns generic dialog mechanics; Dedit describes the three semantic actions and maps their generic outcomes to save/discard/cancel policy. `DeditTheme` creates an instance-only warning `WidgetStyles` overlay from terminal capabilities, while `CloseCoordinator` applies it only to dirty-close dialogs. Clean documents continue to close directly without constructing or showing a modal.

**Tech Stack:** C# 13, .NET 8/9/10, published Icod.Widgets 1.8.0, Icod.DCurses 2.5.2 transitively, xUnit, PowerShell 5.1-compatible packaging tools.

**Spec:** Widgets `docs/superpowers/specs/2026-10-18-generic-dialog-actions-design.md` on the Icod.Widgets 1.8.0 branch.

## Global Constraints

- Do not begin the package rebind until Icod.Widgets 1.8.0 is published and indexed by the configured NuGet source.
- Keep Dedit `Version` and `PackageVersion` at exactly `1.0.0`; require exactly Icod.Widgets `1.8.0` in source, lock assets and packed tool manifests.
- The dirty-close title is `Unsaved Changes — <sanitized caption>` and the body is exactly `Do you want to save these changes?`.
- The action array is: `Yes` / `WidgetDialogOutcome.Yes` / Y; `Don't Save` / `WidgetDialogOutcome.No` / N; `Cancel` / `WidgetDialogOutcome.Cancel` / C / default / cancel.
- Yes saves and closes only after a successful write; No deliberately discards and closes; Cancel, Escape or default Enter retains the document and stops a batch close/exit.
- Style is graphical representation only. It must not select actions, assign shortcuts, map outcomes, change hit regions, or decide whether to save/discard/close.
- Apply the red warning styles only to dirty-document close prompts. About, overwrite, external-change, search, picker and other dialogs retain their existing presentation.
- Keep README concise and version-neutral; put dependency and release history in CHANGELOG, `docs/releases/1.0.0.md`, `docs/Library-Contracts.md` and `docs/Qualification.md`.
- Do not merge, tag or publish.

## Review Focus

- A clean document must close without a modal or warning styles ever entering the interaction path.
- A dirty document's Enter and Escape must both choose Cancel initially; Y, N and C must produce Yes, No and Cancel respectively even though the second caption is `Don't Save`.
- Pointer release on each button must yield the same outcome and `CloseResolution` as its mnemonic/Enter equivalent; no Dedit-specific pointer dispatch is permitted.
- Save failure and Save As requirements must keep the document available; the new generic outcome names must not weaken existing safety checks.
- RGB, indexed-color, partial-color and monochrome terminals must receive legible presentation; capability fallback must not synthesize unsupported color axes.
- Other Dedit uses of the newly obsolete `WidgetMessageDialog.YesNo` must migrate to explicit arrays so Staging/Release warnings remain clean without suppressing CS0618 in product code.

---

### Task 1: Published Widgets 1.8.0 dependency gate

**Files:**
- Modify: `Icod.Dedit.csproj`
- Modify: `packaging/VerifyDeditTool.ps1`
- Modify: `tests/Icod.Dedit.Tests/DeditReleaseIdentityTests.cs`
- Modify: `docs/Library-Contracts.md`

**Interfaces:**
- Consumes: published Icod.Widgets 1.8.0 generic action-array constructor, `Mnemonic`, `Actions`, `SetActionEnabled` and instance `Styles`.
- Produces: exact Dedit restore/package dependency on Icod.Widgets 1.8.0 while retaining Dedit 1.0.0 identity.

- [ ] **Step 1: Confirm the publication gate.** Query the configured NuGet source or restore a temporary exact `Icod.Widgets` 1.8.0 reference. Stop and request owner intervention if 1.8.0 is not indexed; do not use a local package as release evidence.
- [ ] **Step 2: Write failing identity tests.** Rename/update the dependency, verifier and library-contract tests to require exactly `1.8.0`, reject `1.7.2`, and retain Dedit `Version`/`PackageVersion` `1.0.0`.
- [ ] **Step 3: Run and verify RED.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Debug -f net10.0 --filter FullyQualifiedName~DeditReleaseIdentityTests`. Expected: the existing 1.7.2 project, verifier and docs fail the new assertions.
- [ ] **Step 4: Rebind and restore.** Update the project, verifier and library contract to 1.8.0, then run `dotnet restore Icod.Dedit.sln --force-evaluate`. Inspect `obj/project.assets.json` for `Icod.Widgets/1.8.0` and no 1.7.x fallback.
- [ ] **Step 5: Run the identity tests and verify GREEN.** Repeat Step 3. Expected: all release-identity tests pass.
- [ ] **Step 6: Commit.** Run `git add Icod.Dedit.csproj packaging/VerifyDeditTool.ps1 tests/Icod.Dedit.Tests/DeditReleaseIdentityTests.cs docs/Library-Contracts.md && git commit -m "build: rebind Dedit to Widgets 1.8.0"`.

### Task 2: Capability-aware dirty-warning presentation

**Files:**
- Modify: `src/Presentation/DeditTheme.cs`
- Test: `tests/Icod.Dedit.Tests/DeditPresentationTests.cs`

**Interfaces:**
- Consumes: `CursesPresentationCapabilities`, `CursesColor`, `CursesStyle`, `WidgetStyles` and dialog style roles.
- Produces: `internal static WidgetStyles CreateDirtyDialogStyles(CursesPresentationCapabilities capabilities)` containing presentation-only per-instance overrides.

- [ ] **Step 1: Write failing style tests.** Add `RgbDirtyDialogUsesWhiteOnRedAndInvertedFocus`, `IndexedDirtyDialogUsesWhiteOnRedAndInvertedFocus`, `PartialColorDirtyDialogUsesOnlySupportedAxes`, `MonochromeDirtyDialogUsesLegibleAttributeFallback`, and `DirtyDialogStylesOverrideOnlyDialogPresentationRoles`. Assert overrides for dialog surface/title/border/action/default/focused/disabled roles as applicable, with white-on-red ordinary presentation and red-on-white focus where color is supported.
- [ ] **Step 2: Run and verify RED.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Debug -f net10.0 --filter FullyQualifiedName~DeditPresentationTests`. Expected: the style factory does not exist.
- [ ] **Step 3: Implement capability fallback.** Add `CreateDirtyDialogStyles`; use RGB colors when supported, ANSI indexed white/red when at least eight indexed colors are available, and default colors plus supported emphasis/reverse attributes for monochrome. Respect foreground and background capability independently, intersect attributes with the advertised set, and return only immutable `WidgetStyles`; do not accept actions or encode selection policy.
- [ ] **Step 4: Run and verify GREEN.** Repeat Step 2. Expected: all presentation tests pass for RGB, indexed and monochrome fixtures.
- [ ] **Step 5: Commit.** Run `git add src/Presentation/DeditTheme.cs tests/Icod.Dedit.Tests/DeditPresentationTests.cs && git commit -m "feat: add dirty-close warning styles"`.

### Task 3: Generic dirty-close actions and one activation policy

**Files:**
- Modify: `src/Presentation/DeditMenu.cs`
- Modify: `src/Documents/CloseCoordinator.cs`
- Modify: `src/Application/DeditTerminal.cs`
- Test: `tests/Icod.Dedit.Tests/DeditMenuTests.cs`
- Test: `tests/Icod.Dedit.Tests/CloseCoordinatorTests.cs`

**Interfaces:**
- Consumes: Widgets 1.8.0 action arrays and Task 2 warning styles.
- Produces: a Dedit dirty-close dialog factory and `CloseCoordinator` mapping from Yes/No/Cancel to save/discard/retain.

- [ ] **Step 1: Write failing dialog-contract tests.** Add a focused factory test asserting the exact sanitized title/body, action order/captions/outcomes/mnemonics, Cancel default/cancel flags, and supplied warning styles. Assert no other dialog factory receives those warning styles.
- [ ] **Step 2: Write failing close-policy tests.** Replace positional-arrow assumptions with semantic inputs. Cover clean immediate close with no modal, Enter and Escape retaining a dirty tab, Y saving, N discarding, C retaining, pointer clicks on all three buttons producing the same outcomes/resolutions as their mnemonics, Save As required for untitled Yes, and failed/conflicting save retention.
- [ ] **Step 3: Run and verify RED.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Debug -f net10.0 --filter "FullyQualifiedName~DeditMenuTests|FullyQualifiedName~CloseCoordinatorTests"`. Expected: exact action contract, default-Cancel and generic outcome mapping fail against the existing form dialog.
- [ ] **Step 4: Create the dirty-close message dialog.** Add `DeditMenu.CreateDirtyCloseDialog(id, caption, styles)` using three explicit `WidgetDialogAction` values. Sanitize the caption before interpolation, assign instance `Styles`, and keep all save/discard behavior out of the factory and styles.
- [ ] **Step 5: Integrate application policy.** Give `CloseCoordinator` the warning styles (or an explicit dialog factory), show the new message dialog only after `HasUnsavedChanges`, and map `Yes` to the existing guarded save path, `No` to `Close(discardModified: true)`, and all other outcomes to `Kept`. Have `DeditTerminal` construct and pass `DeditTheme.CreateDirtyDialogStyles(session.PresentationCapabilities)`.
- [ ] **Step 6: Run focused and batch tests.** Repeat Step 3, then run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Debug -f net10.0 --filter "FullyQualifiedName~CloseCoordinatorTests|FullyQualifiedName~DocumentBatchCoordinatorTests"`. Expected: exact dialog semantics and close/close-all/exit safety all pass.
- [ ] **Step 7: Commit.** Run `git add src/Presentation/DeditMenu.cs src/Documents/CloseCoordinator.cs src/Application/DeditTerminal.cs tests/Icod.Dedit.Tests/DeditMenuTests.cs tests/Icod.Dedit.Tests/CloseCoordinatorTests.cs tests/Icod.Dedit.Tests/DocumentBatchCoordinatorTests.cs && git commit -m "feat: add themed dirty-close decisions"`.

### Task 4: Remove obsolete factory use and qualify the 1.0.0 candidate

**Files:**
- Modify: `src/Application/DeditTerminal.cs`
- Modify: `src/Presentation/DeditMenu.cs`
- Modify: affected tests under `tests/Icod.Dedit.Tests/`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/releases/1.0.0.md`
- Modify: `docs/Qualification.md`

**Interfaces:**
- Consumes: Widgets 1.8.0 explicit action arrays.
- Produces: warning-free Dedit call sites, concise user guidance, curated release notes and complete package evidence.

- [ ] **Step 1: Write failing migration/release assertions.** Add source/behavior tests requiring no product call to obsolete `WidgetMessageDialog.YesNo`, preserving overwrite's No default/Escape behavior and external-change's existing Keep/cancel policy. Require curated notes and CHANGELOG to mention Widgets 1.8.0, the dirty-only red warning, exact captions, Y/N/C and Cancel default; require README's existing close paragraph to stay concise and version-neutral.
- [ ] **Step 2: Run and verify RED.** Run `dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Staging -f net10.0 --filter "FullyQualifiedName~DeditReleaseIdentityTests|FullyQualifiedName~DeditMenuTests|FullyQualifiedName~SaveAsPickerTests"`. Expected: obsolete calls and stale release text fail, and CS0618 may fail the warnings-as-errors build after the 1.8.0 rebind.
- [ ] **Step 3: Migrate remaining YesNo call sites.** Replace overwrite and external-change uses with explicit two-action arrays whose labels, outcomes, mnemonics, default and cancel behavior reproduce the existing semantics. Update tests to activate semantically rather than depending on button positions. Do not suppress CS0618 in product code.
- [ ] **Step 4: Curate documentation.** Tighten the existing README dirty-close paragraph to say Yes / Don't Save / Cancel, Y/N/C and Cancel default without adding version history. Record Widgets 1.8.0 and complete behavior in CHANGELOG, curated 1.0.0 notes, library contracts and qualification candidate notes.
- [ ] **Step 5: Run full local validation.** Run `./build.sh`, then `pwsh -NoProfile -File packaging/Invoke-Build.ps1 -Configuration Staging` and `dotnet run --project Icod.Dedit.csproj -c Release -f net10.0 -- --version`. Expected: Debug and Staging net8/9/10 restore/build/test/pack/validation pass; Staging has zero warnings/errors; exactly one `Icod.Dedit.1.0.0.nupkg`; manifest dependency exactly `Icod.Widgets/1.8.0`; local tool install/launch/same-version update/uninstall succeeds; version output is `dedit 1.0.0`.
- [ ] **Step 6: Commit.** Run `git add src tests README.md CHANGELOG.md docs && git commit -m "docs: qualify dirty-close dialog behavior"`.

### Task 5: PR update, CI evidence and owner handoff

**Files:**
- Modify: `docs/Qualification.md` only when final measured evidence is available.

**Interfaces:**
- Consumes: Tasks 1–4 and the existing unmerged Icod.Dedit PR.
- Produces: an updated, unmerged Dedit 1.0.0 candidate with three-OS evidence.

- [ ] **Step 1: Push the existing PR branch.** Push the implementation commits to the branch backing Icod.Dedit PR #1; do not open a competing PR and do not merge.
- [ ] **Step 2: Require the full CI matrix.** Verify Linux, Windows and macOS Staging restore/build/test/pack/tool-lifecycle jobs for net8.0/net9.0/net10.0. Inspect logs for exact Widgets 1.8.0 resolution and zero warnings/errors rather than relying only on job conclusions.
- [ ] **Step 3: Run a whole-branch review.** Review from the PR merge base through its final head, focusing on clean-close bypass, save/discard safety, default/cancel behavior, mnemonic/pointer parity, style isolation, obsolete-use removal and documentation accuracy. Fix Critical/Important findings test-first.
- [ ] **Step 4: Record final evidence and stop.** Add the final run URL, commit, test counts and package lifecycle result to `docs/Qualification.md`, push, require the final-head matrix to pass, and report for owner review. Do not merge, tag or publish.
