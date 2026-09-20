# What is Nester

Nester is a compile-time-first UI infrastructure. Its axiom is `state' = f(impulse)`: the level describes *what is*, and only an impulse moves it — the UI is a side effect, never a state source.

## Solution layout

The tree is the source of truth; this section is the map.

**Repository root.**

- `readme.md` is the product's landing page, and the NuGet readme that `Everlong.Nester.Avalonia` and `Everlong.Nester.Wpf` pack through `PackageReadmeFile`.
- `NesterVersion.props` is the product version; every `src/` project imports it, and no test or example does.
- `Directory.Packages.props` is central package management.
- `Directory.Build.props` turns on `EnableWindowsTargeting` whenever the host is not Windows — the switch the two `net8.0-windows` projects need to build there; the condition tests the OS, not the project. An import stops at the nearest file, so `examples/Directory.Build.props` shadows this one.
- `build.cmd` and `build.sh` enter `_build/`, the Nuke host: `Publish`, its default target, packs the packages and the templates into `artifacts/`. Run it after any change there — the gate never compiles or formats `_build/`, which is not in the solution. Keep its package versions literal: `_build/Directory.Packages.props` opts that folder out of central package management.

**Packages.** Ten projects under `src/`. Seven own a package; three declare no package id and do not ship.

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

**tests/.** Five heads, all `net8.0` and xunit v3.

- `Everlong.Nester.Tests` — the platform-free runtime suite, and the source of the shared fixtures the Avalonia head links in.
- `Everlong.Nester.Avalonia.Tests` — the only head with a UI framework (Avalonia headless): shell assembly, chrome, view resolution.
- `Everlong.Nester.Generators.Tests` — the generator and analyzer surface, platform-neutral because generators emit text.
- `Everlong.Nester.Extensions.Tests` — dialog sessions and the auth engine.
- `Everlong.Nester.CodeFixers.Tests` — the code fixes.

**examples/.** Every project here is a development project: it references `src/` through `ProjectReference` and is what you build and run. Nothing in this folder ships.

- `Template.Shared/` has no csproj of its own: each template compiles its sources and links its `Properties/**/*.json*`.
- `Template.Avalonia/` is the shared app body, with `…Desktop/`, `…Browser/` and `…Android/` as hosts over it; `Template.Wpf/` and `Template.TerminalGui/` are the other two bodies.
- `Template.Avalonia/Assets/Fonts/` keeps the embedded CJK subset together with the collection that registers it: the Browser and Android hosts have no system CJK font to fall back on.

**docs/.** `docs/design/` holds the domain specs (HARD RULE 6 governs what goes in them); `docs/guide/` holds the consumer guides.

## ⛔ HARD RULES

1. XML doc (`///`) is governed by the `xml-doc-discipline` skill.
2. Coding style must follow `.editorconfig` (indent with 2 spaces/ LF line end/ utf-8 encoding/ insert final new line)
3. Never run `dotnet format` solution-wide: it rewrites files you never touched and buries the diff.
4. Projects under `src/` and `tests/` write their imports per file: no global usings without a hard reason, because a global using hides where a file's dependencies come from. One reason qualifies: a single-source file compiled into two platform packages has to resolve the same name on both sides, so the platform projects declare the aliases that name their UI framework globally (`src/Everlong.Nester.Avalonia/GlobalUsings.avalonia.cs`). Aliases only — never a namespace.
5. Tests under `tests/` are filed under the domain they test: directory `tests/Everlong.Nester.Tests/<Domain>/` and namespace `Everlong.Nester.Tests.<Domain>`.
6. `docs/design/*.md` states the domain's design intent and what the code cannot express — never a restatement of the code, and never a reference to a type, word or document of a domain the code does not depend on.
7. The analyzers ride in `Everlong.Nester` alone: it packs the generator and code-fix DLLs into `analyzers/dotnet/cs`, and every other package reaches them through the core. A second copy would deliver each diagnostic twice.
8. A file compiled into two platform packages lives in the Avalonia one and is linked by the WPF one (`<Compile Include>` + `Link`), never copied: `src/Everlong.Nester.Avalonia/` for the core chrome, `src/Everlong.Nester.Extensions.Avalonia/` for the extension chrome and the animation kit.
9. Template content must not contain `#if`/`#endif`: the dotnet-new engine evaluates them against template symbols and silently strips the guarded block. A file one platform must drop is excluded in the consuming csproj (`<Compile Remove>`), never guarded in the source.

## Branches

Day-to-day work lands on `dev`; `main` is what a release is cut from, and it advances by merging rather than by committing. The release workflow enforces the second half: a stable `v*` tag must be an ancestor of `origin/main`, so a tag placed on a `dev` commit is refused before anything is packed.

`main` carries a ruleset: no deletion, no force push, and every change through a pull request whose `gate` and `templates` checks are green. A pull request is merged by squash, which makes its title the subject of the one commit it contributes and its body that commit's body — both are read as a commit message, and the Commit Gate's rules apply to them. `dev` is reset onto `main` after each merge, so the next pull request carries only its own work.

## Commit Gate

1. Run `./.agents/commit-gate.sh` (usage: `--help`) before a commit, and only before a commit: it checks the pending change, so with nothing pending it refuses, and it is not a build runner — `dotnet build` and `dotnet test` are. `--all` checks every project, for a push or a release.
2. The gate's policy — which diagnostics fail — lives in `.editorconfig`; the script only runs the toolchain.
3. Format the commit message according to the `commit-messages` skill, and let the gate check it (`--message <file>`).
