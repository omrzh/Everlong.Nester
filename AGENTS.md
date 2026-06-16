# What is Nester

Nester is a compile-time-first UI infrastructure. Its axiom is `state' = f(impulse)`: the level describes *what is*, and only an impulse moves it — the UI is a side effect, never a state source.

## ⛔ HARD RULES

1. XML doc (`///`) is governed by the `xml-doc-discipline` skill.
2. Coding style must follow `.editorconfig` (indent with 2 spaces/ LF line end/ utf-8 encoding/ insert final new line)
3. Never run `dotnet format` solution-wide: it rewrites files you never touched and buries the diff.
4. Projects under `src/` and `tests/` write their imports per file: no global usings without a hard reason, because a global using hides where a file's dependencies come from. One reason qualifies: a single-source file compiled into two platform packages has to resolve the same name on both sides, so the platform projects declare the aliases that name their UI framework globally (`src/Everlong.Nester.Avalonia/GlobalUsings.avalonia.cs`). Aliases only — never a namespace.
5. Tests under `tests/` are filed under the domain they test: directory `tests/Everlong.Nester.Tests/<Domain>/` and namespace `Everlong.Nester.Tests.<Domain>`.
6. `docs/design/*.md` states the domain's design intent and what the code cannot express — never a restatement of the code, and never a reference to a type, word or document of a domain the code does not depend on.

## Commit Gate

1. Run `./.agents/commit-gate.sh` (usage: `--help`). The gate's policy — which diagnostics fail — lives in `.editorconfig`; the script only runs the toolchain.
2. Format the commit message according to the `commit-messages` skill, and let the gate check it (`--message <file>`).
