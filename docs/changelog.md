# Changelog

An entry is written per release, newest first, and says what a consumer has to act on — a required
migration, a renamed package, a raised framework floor — not what the commit log did. The version is
`AppVersion` in `NesterVersion.props`, and the seven packages and the template package carry it.

Below 1.0 nothing is promised stable: a minor version may rename a type or change a contract, and an
entry says so when it does. That is the one thing a consumer should assume rather than read here.

## 0.1.9 — 2026-09-21

The shell's intent family is split in two, one intent is gone, and the flying plane gains an anchor.

**Migrate.** `IShellIntent` carried two subjects under one name — the window's chrome and the shell's own
end. Six records move to the new `IWindowIntent`: `ShowIntent`, `HideIntent`, `TopmostIntent`,
`MutateShellStateIntent`, `RestoreShellStateIntent` and `CenterOnScreenIntent`. What `IShellIntent` keeps
is the shell's end alone — `TryCloseIntent` and `CloseIntent` are unchanged.

A platform shell or an application shell that answers either set has to widen its guard: a fallback branch
that tested `is not IShellIntent` now answers nothing in the window family, and a switch that tested a
narrowed `IShellIntent` stops compiling (`CS8121`).

`CenterOnOwnerIntent` is removed. No platform implemented it, so a dispatch of it settled as `Pass`
everywhere — `Everlong.Nester.TerminalGui` "consumed" it as a no-op and no window platform had a case
for it at all.

**Behaviour.** `Everlong.Nester.TerminalGui` no longer consumes the window intents. It has no window, so
a window intent settles as `Pass` instead of `Handled`; read `IntentResult` where the answer matters.

**Behaviour.** The reveal waits for the arriving view to be laid out before the scene director runs: one
dispatcher pass, and the view's `Loaded` event when one pass was not enough. `ISceneTransition`'s "laid
out" promise now holds for a dialog's arriving view too, so a geometry-based director — a shared-element
flight — no longer needs a wait of its own, and a director that read its own `Bounds` through a
hand-rolled dispatcher loop can drop it.

**Migrate.** `TransitionContext.FlyingCanvas` is `FlyingCanvas?` instead of `PCanvas`, and it is
genuinely `null` outside page composition. A notice director was handed a null plane with a
non-nullable annotation before, so a director that reads geometry off it needs a null check.

**Behaviour.** The flying plane carries an untyped anchor: `FlyingCanvas.Anchor` is one value the
plane never reads or validates, and it is the window-scoped place for what the view layer knows at a
tap and the arriving director needs afterwards — a shared-element origin, for instance. It is cleared
with the surface, by `FlyingCanvas.Clear()` and when the plane's lease is reclaimed, and reading it does
not take it.  Child visuals go on `Children` as before.

## 0.1.7 — 2026-09-20

The first release, seven packages at one version plus the template package:

| packages | target |
|---|---|
| `Everlong.Nester`, `.Abstractions`, `.Extensions`, `.Avalonia`, `.Extensions.Avalonia` | net8.0 |
| `Everlong.Nester.Wpf`, `.Extensions.Wpf` | net8.0-windows |
| `Everlong.Nester.Templates` | the `nester-avalonia` and `nester-wpf` templates |

Nothing to act on: nothing precedes this version, so nothing is retired, renamed or raised.
`Everlong.Nester.TerminalGui` is not published — it is the experimental surface — and `Generators`
and `CodeFixers` ship inside `Everlong.Nester` as analyzers rather than as packages of their own.
