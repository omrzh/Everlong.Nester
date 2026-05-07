# What is Nester

Nester is a compile-time-first UI infrastructure. Its axiom is `state' = f(impulse)`: the level describes *what is*, and only an impulse moves it — the UI is a side effect, never a state source.

## ⛔ HARD RULES

1. XML doc (`///`) is governed by the `xml-doc-discipline` skill.
2. Coding style must follow `.editorconfig` (indent with 2 spaces/ LF line end/ utf-8 encoding/ insert final new line)
3. Reduce warning noise during middle steps by `dotnet build Everlong.Nester.slnx -v:q --nologo -clp:ErrorsOnly` or a chained `tail` command.
4. Never run `dotnet format` solution-wide: it rewrites files you never touched and buries the diff.
5. Projects under `src/` and `tests/` write their imports per file: no global usings without a hard reason, because a global using hides where a file's dependencies come from.
6. Tests under `tests/` are filed under the domain they test: directory `tests/Everlong.Nester.Tests/<Domain>/` and namespace `Everlong.Nester.Tests.<Domain>`.

## Common Traps

1. **`dotnet test` accepts `-v` and nothing else.** The build noise flags (`--nologo`, `-clp:*`, `-tl:*`) are MSBuild flags; under the MTP runner that `global.json` selects they are unknown options, and the run reports zero tests and exits 5 without ever naming the flag responsible. Keep them on `dotnet build`. The working invocation is `dotnet test --solution Everlong.Nester.slnx`, and an empty `TestResults/` left in the working directory is the sign that a run really engaged the runner.

## Commit Gate

1. Ensure changed files are formatted with `dotnet format` and free of build warnings/diagnostics, specifically `IDE0005`, `CS1574`, and `CS0105`.
2. Format the commit message according to the `commit-messages` skill.
