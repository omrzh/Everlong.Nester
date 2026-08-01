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

The design specs live in `docs/design/`.

## License

MIT. See `LICENSE` and `ThirdPartyNotices.txt`.
