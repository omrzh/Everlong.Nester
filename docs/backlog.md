# Backlog

Known work, none of it scheduled. An entry carries its evidence — a path, a command, a number — or it does
not belong here, and an entry is deleted when it is done, never struck through.

## Decided, unscheduled

- **The generic `IntentCommand` posts nothing.** `src/Everlong.Nester.Wpf/Shell/IntentCommands.cs:28` has no
  `CommandBinding` anywhere; `RegisterCommandBindings` binds the five shell-state commands only.
  `PostIntent(control, intent)` is the static path a binding would call. Bind it or drop it.
- **`DialogShake.PlayBlockedSound` is an empty body on Avalonia**
  (`src/Everlong.Nester.Extensions.Avalonia/Controls/DialogShake.cs:23`), where the WPF twin plays
  `SystemSounds.Beep`. Public API whose caller is a consumer, so not dead code: wire a playback path.
- **Analyzer release tracking is weaker than it reads.** `Microsoft.CodeAnalysis.Analyzers 3.3.4` arrives
  transitively and nothing pins it; `.editorconfig` sets no `RS*` severity, so `RS2000`–`RS2008` cannot fail a
  build (they do fire when provoked); `Generators.csproj:25-26` removes `AdditionalFiles` the package's targets
  include first, so the pair removes nothing.
- **Absorption at package level is untested.** The absorbed-revision flow, the transfer's two moving sides and
  the re-armed `IArriving` case run from HEAD source only, and the moving sides have never run on TerminalGui
  (`tests/Everlong.Nester.Tests/Routing/`).
- **Two absorption shapes have no consumer-facing page.** Layout args are generated and pinned
  (`GeneratesRouteWithParameterProjection`) but absent from `docs/guide/navigation.md`; a page that consumes
  `BackIntent` and re-engages itself is written down nowhere. `docs/design/absorption.md` §1 has both shapes.
- **Three threads were never written down**: the focus-declared-by-the-view intent behind `FocusPolicy`
  (`StagePanel` maps it by stack position); the release-accounting numbers behind `PinChain` / `InstancePins`
  (`docs/design/routing.md` §9 has the shape, the `StackShapes` drill was dropped); the RouteSync matcher audit
  and performance numbers (`docs/design/routesync.md` §3 has the mechanism).
- **`CleanOutput` wipes `artifacts/`**, so a single target run leaves a partial output directory
  (`.\build.cmd PackTemplates` leaves only the template package). `Publish` is unaffected.
- **The template smoke test's cleanup is best-effort** — an IDE build host holds a freshly generated project
  open, so a run leaves it behind and the next one sweeps it (`min_age=900`).
- **Without an Android SDK the Android example host is skipped, silently.** MSBuild reports
  `Skipping Template.Avalonia.Android: no Android SDK found` and exits 0 — so neither the build nor `dotnet
  format` judges it, and a green run says nothing about that host. Closing it costs ~50s and ~800 MB:
  `dotnet build examples/Template.Avalonia.Android/Template.Avalonia.Android.csproj` with
  `-t:InstallAndroidDependencies -f net10.0-android`, `AndroidSdkDirectory` and `JavaSdkDirectory` set and
  `AcceptAndroidSDKLicenses=true`, then both paths exported so MSBuild reads them.

## Open verdicts

- **`dialog.md`'s chrome contract** — the theme-key table (`Nester.DialogChrome.*`, `Nester.Dialog.Width.*`,
  `Nester.ImagePreview.*`) went when the spec was reduced. Consumer contract, or platform chrome restating the
  code?
- **Who owns the platform packages' chrome.** `Extensions.*` took the animation kit; the platform packages
  still ship `TreeControl` / `TreeItem`, their themes and three palette keys nothing consumes. Measure
  consumer impact first: WPF resolves a control's default template from its own assembly, and the palette is
  shared. Payoff: `Everlong.Nester.Wpf` would need no `InternalsVisibleTo`.
- **Whether to make the test tiers explicit** — parallelization is off in three heads, 27
  `[Collection("RealShell")]`, no `Trait`. Three options with their costs; landing it means a HARD RULE.
- **What `AGENTS.md` still owes.** Reviewed once: the `.agents/` pointer and a rule that documentation follows
  the change are the two genuine gaps — the CPM and "readme is the package page" candidates are already
  expressed by `Directory.Packages.props` and `AGENTS.md` itself. The bar for a line there, given once: no
  description nobody would ask for, and where code already expresses the intent, one mechanism sentence and a
  pointer.
- **macOS has never built this tree.** `ci.yml` verifies Linux on every push and the Windows gate runs locally,
  so what remains unrun is a Mac. `build.sh` and the workloads are the parts that could differ.
  *Decided by: someone running it, or dropping the claim.*
- **`CloseIntent` is not as uncancellable as its old doc promised.** The pair differs only in which handler
  probes it: the app Director's guard is keyed on `TryCloseIntent`
  (`examples/Template.Shared/Pages/Shell/MainViewModel.cs:119`), while any presented page vetoes either one
  through the chain consultation `RouterBase.HandleAsync` runs first
  (`src/Everlong.Nester/Routing/RouterBase.cs:592`). The claim the doc used to make is a property of the
  sender, not of the record. Either the teardown leaves the intent path for a direct shell-teardown entry,
  or one record is enough and `TryCloseIntent`'s `Try` is the whole difference.
  *Decided by: a caller that needs a close nothing can refuse.*
- **`HostState` names one thing three ways.** Its doc says "Window state for the shell"
  (`src/Everlong.Nester/Primitives/HostState.cs:4`), its dispatch site is `MutateShellStateIntent`, and the
  snapshot that carries it is `HostPropertyBase` — whose own members say "shell-state" and "shell title".
  The honest name is out of reach at the moment: `src/Everlong.Nester.Wpf/Shell/ShellExtensions.cs`
  imports both `System.Windows` and `Everlong.Nester.Primitives`, so a Nester `WindowState` would be
  ambiguous there — the `PWindowState` alias adds a name and removes none.
  *Decided by: moving the platform alias out of the way, or accepting a third name.*

## Considering

Undecided. An entry states what would decide it; one that cannot is dropped rather than parked.

- **Warm pool — do not raise it unless the user does.** Preheating part of a page's first frame during a
  splash: the stages already exist (resolve / ensure-views / present / reveal), so it is one extra state plus
  a promote. If picked up: never touch the history stack or fire navigation lifecycle, a real navigation
  always wins, reclaim under memory pressure. Levels: resolve-only → ensure-views → attach-hidden.
  *Decided by: the user asking for it.*
- **xunit v4.** Tried against `xunit.v3 4.0.1`: three suites pass, and `Everlong.Nester.Avalonia.Tests` loses
  all 189 `[AvaloniaFact]` / `[AvaloniaTheory]` cases because `Avalonia.Headless.XUnit` 12.1.2 binds
  `xunit.v3.extensibility.core 3.2.2`. `Verify` stays at 32.x.
  *Decided by: `Avalonia.Headless.XUnit` moving to `4.x`.*

## Not doing

Decided against, recorded so the question is not opened twice.

- **Wording drift in `IShell.cs` / `ShellBase.Startup.cs`** — "presentation anchor", "chain".
- **Tests for `Primitives` / `Helpers`** — deliberately uncovered for now.
- **Package dependencies in `ThirdPartyNotices.txt`** — it records incorporated source alone.
- **A clickable link in the readme** — a package page resolves no relative path, and the documentation gate
  reads an inline link as a reference without a definition.
- **A WPF view-resolution test head** — WPF has no headless host in this repository, so the order the WPF
  chain walks is fixed by the remark on `…/Controls/DataTemplatePrecedenceTests.cs`, not by an assertion;
  the platform rule itself is `docs/design/view-resolution.md` §6.
- **Runtime test heads for the Android and Browser example hosts** — either needs a device or a browser
  runner, and what the repository owes an example host is that it compiles, which is what the two workloads
  in `ci.yml` check.
- **A test head for `Everlong.Nester.TerminalGui`** — the surface is experimental and unpackable, so a head
  would pin behaviour the API is explicitly free to change before 1.0.
- **A post-publish consumption check in `release.yml`** — the job already installs and builds the packed
  template before it pushes, so a step that ran after the push could only report what had already shipped.
- **A `windows-latest` job for the WPF template case** — `template_smoke.py` skips it off Windows, and this
  machine runs it; a runner would re-prove a case that is not the one that breaks.
- **Caching NuGet packages in CI** — the restore is not where the minutes are; the workload install is.
- **Restating the design-document ruler in `AGENTS.md`** — HARD RULE 6 states its scope and the fourteen specs
  are the shape samples a fifteenth is written from; the mechanical half has never been the thing that broke.
