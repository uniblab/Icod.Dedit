# Released library contracts used by dedit

Status: D1001 integration review draft. These are existing public capability
anchors, not a request for new APIs. Keep application policy in dedit. The
released [Icod.DCurses](https://github.com/uniblab/Icod.DCurses) and
[Icod.Widgets](https://github.com/uniblab/Icod.Widgets) packages cover the
core session, editing, workspace and theme needs. Verify each finer behavior
against the package in its D1002+ slice. The 1.0.0 candidate's direct dependency is
`Icod.Widgets 1.7.1`, which declares
`Icod.DCurses 2.5.2`; a direct DCurses reference is added only if compiling
against its public session types requires it.

| Slice | Public capability to consume | dedit responsibility / gate |
| --- | --- | --- |
| D1002 lifecycle | `CursesSession.OpenAsync`, `ReadEventAsync`, `RefreshAsync`, `SynchronizeDimensions`, `Invalidate`, async disposal; `WidgetHost` owns a tree, `Process(input, elapsed)`, `Render`, `DrainActions`. | One caller-owned session and loop. Acquire/release input protocol leases; restore screen, cursor and input modes after normal exit or failure. Help/version/invalid CLI never opens a terminal. |
| D1003 editing | `WidgetTextDocument`, `WidgetTextViewState`, `WidgetTextEditor`, text snapshots/revisions and editor actions; DCurses text width/layout services. | Dedit owns document identity and view lifetime. Measure many-line/long-line behavior before promising a size limit; no whole-file pad. |
| D1004 commands | `WidgetMenuBar`/`WidgetMenuItem` with separate mnemonics and shortcuts, dynamic presentation; `WidgetHost` focus/modal and focused edit commands; captioned dialogs including Cancel on Escape. | Route all gestures through application command IDs and supply time. F6 is an application binding. Clipboard results return through the app loop. |
| D1005 tabs/files | `WidgetTabStrip` with stable keys and activation/close requests; `WidgetContentHost`; provider-backed pickers and `WidgetSaveTargetPicker`. | Dedit reads/writes bytes and owns tabs-to-document binding, collision policy and encoding. Pickers return paths and observations, never save files. |
| D1006 close/save | `WidgetFormDialog.SaveDontSaveCancel` and other typed dialog results. | Close coordinator performs the guarded save, then closes only on success or deliberate discard. Never infer save completion from dialog choice alone. |
| D1007 search/status | `WidgetSearchForm`, `WidgetGoToLineDialog` (line only), composable text fields/forms, `WidgetStatusBar` actionable fields. | Dedit executes search and validates revision. Its line:column input uses a composed form because the existing Go to Line dialog accepts only a line number. Dedit performs file-format conversion and supplies status values. |
| D1008 presentation | `WidgetPalette.Create(session.PresentationCapabilities)`, `WidgetHost.SetTheme`, `CursesSession` clipboard and cursor-style leases. | Use advertised capabilities and bounded clipboard operations; text/default fallback stays usable. Test actual terminal output separately from headless rendering. |

Existing release/API references:
[Widgets 1.7 README](https://github.com/uniblab/Icod.Widgets/blob/v1.7.1/README.md),
[Widgets public API baseline](https://github.com/uniblab/Icod.Widgets/blob/v1.3.0/docs/Public-API-Baseline-1.3.md),
[DCurses 2.5.2 README](https://github.com/uniblab/Icod.DCurses/blob/v2.5.2/README.md).

No further library feature is assumed for D1002. If D1003+ tests expose a
missing reusable behavior or unacceptable performance, propose that change in
the owning library's PR and consume its released version; do not copy its
implementation into dedit. The packages being published does not substitute
for integration tests in this repository. In particular, linewise clipboard
semantics, double/triple-click selection and line:column form composition are
application acceptance requirements, not claims about the dependency package.
