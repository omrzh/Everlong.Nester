# What is Nester

Nester is a compile-time-first UI infrastructure. Its axiom is `state' = f(impulse)`: the level describes *what is*, and only an impulse moves it — the UI is a side effect, never a state source.

## ⛔ HARD RULES

1. XML doc (`///`) is governed by the `xml-doc-discipline` skill.
2. Coding style must follow `.editorconfig` (indent with 2 spaces/ LF line end/ utf-8 encoding/ insert final new line)
3. Reduce warning noise during middle steps by `dotnet build Everlong.Nester.slnx -v:q --nologo -clp:ErrorsOnly` or a chained `tail` command.
4. Never run `dotnet format` solution-wide: it rewrites files you never touched and buries the diff.
5. Projects under `src/` and `tests/` write their imports per file: no global usings without a hard reason, because a global using hides where a file's dependencies come from. One reason qualifies: a single-source file compiled into two platform packages has to resolve the same name on both sides, so the platform projects declare the aliases that name their UI framework globally (`src/Everlong.Nester.Avalonia/GlobalUsings.avalonia.cs`). Aliases only — never a namespace.
6. Tests under `tests/` are filed under the domain they test: directory `tests/Everlong.Nester.Tests/<Domain>/` and namespace `Everlong.Nester.Tests.<Domain>`.
7. `docs/design/*.md` states the domain's design intent and what the code cannot express — never a restatement of the code, and never a reference to a type, word or document of a domain the code does not depend on.

## Common Traps

1. **`dotnet test` accepts `-v` and nothing else.** The build noise flags (`--nologo`, `-clp:*`, `-tl:*`) are MSBuild flags; under the MTP runner that `global.json` selects they are unknown options, and the run reports zero tests and exits 5 without ever naming the flag responsible. Keep them on `dotnet build`. The working invocation is `dotnet test --solution Everlong.Nester.slnx`, and an empty `TestResults/` left in the working directory is the sign that a run really engaged the runner.
2. **`IDE0031` (null propagation) never appears in build output — only in `dotnet format`.** Unlike `IDE0005`, raising it to `warning` does not make `EnforceCodeStyleInBuild` surface it: a full `dotnet build` stays silent while `dotnet format <proj> --verify-no-changes` reports `warning IDE0031` and exits 2. Its severity lives on its own option line (`dotnet_style_null_propagation = true:warning`, `.editorconfig:58`), because an option-backed rule carries its severity there while an ID-only rule (`IDE0005`, `CS0105`, `CS1574`) goes in the `dotnet_diagnostic.*` block. At the default `suggestion` the format gate passes with the violation still in the file — that is how six sites in the Avalonia controls drifted out of shape.
3. **`-clp:ErrorsOnly` hides `IDE0005` too.** HARD RULE 3 quiets the middle steps with that switch, but a build run under it reports nothing about an unused using, so a warning-free check has to drop the switch (`--no-incremental`, no `-clp:`) or rely on the `dotnet format` half of the gate. This bites when a global alias loses its last consumer: the plain build names it, the quiet one does not.

## Commit Gate

1. Ensure changed files are formatted with `dotnet format` and free of build warnings/diagnostics, specifically `IDE0005`, `CS1574`, `CS0105`, and `IDE0031`.
2. Format the commit message according to the `commit-messages` skill.
