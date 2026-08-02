# Solution layout — the map and the seams

> Status: living spec. The tree is the source of truth; this document fixes the map and
> the seams the tree cannot state — not a second copy of the projects.

## 1. Repository root

`NesterVersion.props` is the product version: every `src/` project imports it, and no test
or example does — a test reports the version of what it references.
`Directory.Packages.props` is central package management.

Off Windows, the root `Directory.Build.props` turns on `EnableWindowsTargeting` for the
two WPF projects. An import stops at the nearest file, so `examples/Directory.Build.props`
shadows this one — which is how the examples come to carry a copy of that property of
their own.

The other root files need no introduction: `global.json` (the test runner),
`Everlong.Nester.slnx`, `.editorconfig`, `LICENSE`, `ThirdPartyNotices.txt`. `readme.md` is
the exception: it is the product's landing page, and the NuGet readme that
`Everlong.Nester.Avalonia` and `Everlong.Nester.Wpf` pack through `PackageReadmeFile`.

## 2. Packages

Ten projects under `src/`. Seven own a package; three declare no package id and do not
ship.

| project | TFM | what it is |
|---|---|---|
| `Everlong.Nester.Abstractions` | net8.0 | the contracts |
| `Everlong.Nester` | net8.0 | the runtime |
| `Everlong.Nester.Extensions` | net8.0 | the opinionated layer |
| `Everlong.Nester.Avalonia` | net8.0 | the Avalonia surface |
| `Everlong.Nester.Wpf` | net8.0-windows | the WPF surface |
| `Everlong.Nester.Extensions.Avalonia` | net8.0 | the default chrome |
| `Everlong.Nester.Extensions.Wpf` | net8.0-windows | the same surface on WPF |
| `Everlong.Nester.TerminalGui` | net10.0 | the experimental terminal surface |
| `Everlong.Nester.Generators` | netstandard2.0 | the generators and analyzers |
| `Everlong.Nester.CodeFixers` | netstandard2.0 | the code fixes |

**Boundaries.** `Abstractions` is contracts alone and declares no package of its own:
`Everlong.DI` belongs to the package whose code uses it. The core carries no MVVM toolkit
and none of the dialog, notice, route-sync or auth domains. The platform packages carry no
dialog or notice surface.

**Where the analyzers live.** `Everlong.Nester` references the two analyzer projects, and
it is the only package that packs them, into `analyzers/dotnet/cs`.

**Where the shared files live.** The Avalonia project of each pair is the home, and the
WPF project compiles those files through `Link`.

## 3. tests/

Five heads, all `net8.0` and xunit v3.

- `Everlong.Nester.Tests` — the platform-free runtime suite, and the home of the test kit
  the Avalonia head links from.
- `Everlong.Nester.Avalonia.Tests` — the only head with a UI framework (Avalonia
  headless): shell assembly, chrome, view resolution. It also compiles the template's
  shared sources and the Avalonia partials, and generates its own `Lang` set from the
  template's i18n.
- `Everlong.Nester.Generators.Tests` — the generator and analyzer surface, platform-neutral
  because generators emit text.
- `Everlong.Nester.Extensions.Tests` — dialog sessions and the auth engine.
- `Everlong.Nester.CodeFixers.Tests` — the code fixes.

`Everlong.Nester.Avalonia.Tests` pins its root namespace to `Everlong.Nester.Tests`.

## 4. examples/

Every project here is a development project: it references `src/` through
`ProjectReference` and is what you build and run. Nothing in this folder ships.

- `Template.Shared/` has no csproj of its own: each template compiles its sources and links
  its `Properties/**/*.json*`.
- `Template.Avalonia/` is the shared app body, with `…Desktop/`, `…Browser/` and
  `…Android/` as hosts over it; `Template.Wpf/` and `Template.TerminalGui/` are the other
  two bodies.
- `Template.Avalonia/Assets/Fonts/` keeps the embedded CJK subset together with the
  collection that registers it: the Browser and Android hosts have no system CJK font to
  fall back on.

## 5. docs/

`docs/design/` holds the domain specs; `AGENTS.md` governs what may go in them. This file
is the only document under `docs/` that is not a spec.
