# Changelog

An entry is written per release, newest first, and says what a consumer has to act on — a required
migration, a renamed package, a raised framework floor — not what the commit log did. The version is
`AppVersion` in `NesterVersion.props`, and the seven packages and the template package carry it.

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
