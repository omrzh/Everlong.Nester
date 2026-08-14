# Nester

**Compile-time-first UI infrastructure for Avalonia, WPF and Terminal.GUI.**

Nester packages the hardest-to-unify pieces of desktop UI into one intent-driven
infrastructure — navigation, layouts, dialogs, feedback, authorization, member
injection, settings, i18n and app lifecycle. Wiring is resolved by source generators
at compile time, so the runtime stays small.

The axiom is `state' = f(impulse)`: the UI is a side effect, never a state source. A user
gesture becomes an intent before it can change anything, and the state change commits
before the UI converges onto it.

- **A navigation target is a ViewModel type, not a route record.** `[Layout<T>]` declares
  the shell chain, `[Routable<TArgs>]` the typed arguments, and the generator bakes both
  into a sealed locator.
- **One graph for every surface.** Page navigation, dialogs, toast/snackbar/notification,
  floating pages and authorization are one service graph — not a patchwork of libraries.
- **Shell commands are intents.** Back, forward, refresh and close travel as `IIntent`
  through `IShell.DispatchIntent`, so a page can veto a navigation and no layout control
  navigates directly.
- **Two platforms, one body of code.** Avalonia and WPF share the same abstractions; WPF
  compiles the Avalonia-side single-source files instead of forking them. Terminal.GUI is
  an experimental surface.

## Start from the template

The project template is the primary entry point, and the generated project *is* the tutorial: a
working app whose pages, dialogs, settings and translations are meant to be edited into yours. Every
file it produces is a starting point you keep, not scaffolding you delete.

```
dotnet new install Everlong.Nester.Templates

dotnet new nester-avalonia -n MyApp    # Avalonia desktop; `nester-wpf` for WPF
cd MyApp
dotnet run
```

The Avalonia browser and Android hosts are repository examples rather than packaged templates: the
single-view branch they need is already in the generated `App.axaml.cs`.

### Tour

Open the generated project and read it in this order. `[all]` = identical in both templates,
`[Av]` / `[Wpf]` = the platform's own copy. Every path is relative to the generated project root.

| # | File | What it is — and the seam you will touch |
|---|---|---|
| 0 | `Program.cs` `[Av]` | The platform entry point. WPF generates its `Main` from `App.xaml` and has no separate file. Untouched. |
| 1 | `App.axaml.cs` `[Av]` · `App.xaml.cs` `[Wpf]` | The bootstrap, in order: build the process container → single-instance negotiation → `AppLifetimeBuilder.Build` → start the first shell and declare it `SetMainShell` → `Lang.Initialize`. It also owns the `DataTemplates` translation table (your custom hook → the generated locator → the framework views). **Your seam:** process-level services, the activation agent, the startup route. |
| 2 | `UiShell.cs` `[all]` | Your shell class — one class for every shell kind, the kind declared per instance by `DirectorType`. Marked ① registrations / ② provider / ③ post-assembly: the window's container, the host window, and the ready anchor where settings are injected. **Your seam:** what a window registers, which host it shows. |
| 3 | `ViewLocators.cs` `[all]` | The `[ViewLocator]` trigger, and the `[Mapping<TModel,TView>]` list for views that live in another assembly. **Your seam:** cross-assembly views. |
| 4 | `Hosting/AppServices.cs` `[all]` | The `[ServiceRegistrar]`: one class collects every `[Singleton<T>]` / `[Transient]` / `[Scoped<T>]` in the project. **Your seam:** your services — annotate them and they register themselves. |
| 5 | `Pages/Shell/MainViewModel.cs` `[all]`, `MainViewModel.Avalonia.cs` `[Av]`, `MainViewModel.Wpf.cs` `[Wpf]` | The `IShellDirector`: which first page a startup input decides, how an activation (deep link / file) is routed, where errors land. The shared part holds the decisions, the platform partial the platform cases. **Your seam:** deep links and the startup route. |
| 6 | `Pages/Shell/MainLayoutModel.cs`, `MainLayout.axaml` `[Av]`, `MainLayout.xaml` `[Wpf]` | The persistent chrome: the `RouteItem` navigation tree, back / forward / refresh dispatched as intents, logout shell swapping, `Authorize.Visible` on the admin shortcut, and a sidebar that narrows to a 64 px rail below a 700 px window (`n:Responsive.CompactBelow`) — one menu declaration the two forms project differently. **Your seam:** the menu and the chrome. |
| 7 | `Pages/Landing/LandingPageModel.cs`, `LandingPage.axaml` `[Av]`, `LandingPage.xaml` `[Wpf]` | The smallest complete page. **Your seam:** copy it for page two. |
| 8 | `Pages/Posts/PostsPageModel.cs`, `Pages/Posts/PostDetailPageModel.cs` `[all]` | `IArrived` data loading, `[Routable<TArgs>]` typed arguments, and a route highlight that claims a section it does not navigate to. **Your seam:** parameterized pages and list→detail. |
| 9 | `Properties/AppSettings.cs`, `Properties/i18n/en/*.json` `[all]` | `[Settings]` / `[Section]` / `[Setting]` typed persistence, and the locale files the `Lang` classes are generated from. **Your seam:** your settings and your strings. |
| 10 | `Dialogs/*.cs` (e.g. `MyConfirmDialogSession.cs`), `Pages/Labs/InteractionLabPageModel.cs` `[all]` | A custom session (`DialogSessionBase<T>`, `Close(result)`) and the interaction lab — the one page that exercises every dialog, notice channel and ICU message. **Your seam:** your dialogs; the lab is the reference. |
| 11 | `Services/JsonSettingsStore.cs`, `Services/AuthRegistry.cs` `[all]` | `[Singleton<ISettingsStore>]` persistence and the `[AuthRegistry]` policy binding, plus `AuthorizedRouter`, the router subclass that evaluates `[Authorize]` on every route. **Your seam:** persistence and policies. |
| 12 | `Services/WorkspaceFileMonitor.cs`, `Dialogs/WorkspacePaletteSession.cs`, `Dialogs/WorkspacePaletteView.axaml` `[Av]`, `Dialogs/WorkspacePaletteView.xaml` `[Wpf]` | The workspace domain: a `FileSystemWatcher` whose root is *resolved* rather than configured (nearest `.git` → a solution or project file → Downloads), which broadcasts its changes on the hub and releases its OS handle through `IHostLifetime.Stopping` — no shell, no app lifetime. `Ctrl+Shift+P` presents the palette on a derived router, so `AuthorizedRouter` gates it like any other surface. **Your seam:** what the workspace is, what Enter opens, and how the palette arrives. |

---

## Packages

| Package | What it is |
| --- | --- |
| `Everlong.Nester.Abstractions` | the contracts every other package builds on |
| `Everlong.Nester` | the core runtime, and the only package that carries the generators |
| `Everlong.Nester.Avalonia` | the Avalonia platform surface |
| `Everlong.Nester.Wpf` | the WPF platform surface |
| `Everlong.Nester.Extensions` | the opinionated layer: component-model bases, dialog sessions, notice and route-sync contracts, authorization |
| `Everlong.Nester.Extensions.Avalonia` | the default views and controls for those domains, on Avalonia |
| `Everlong.Nester.Extensions.Wpf` | the same surface on WPF |

## Install

```
dotnet add package Everlong.Nester.Avalonia    # or Everlong.Nester.Wpf
```

## Documentation

The guides — `docs/guide/get-started.md`, `docs/guide/navigation.md`, `docs/guide/interaction.md` —
and the design specs under `docs/design/`.

## License

MIT. See `LICENSE` and `ThirdPartyNotices.txt`.
