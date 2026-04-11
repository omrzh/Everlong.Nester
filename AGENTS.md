# What is Nester

Nester is a compile-time-first UI infrastructure. Its axiom is `state' = f(impulse)`: the level describes *what is*, and only an impulse moves it — the UI is a side effect, never a state source.

## ⛔ HARD RULES

1. XML doc (`///`) is governed by the `xml-doc-discipline` skill.
2. Coding style must follow `.editorconfig` (indent with 2 spaces/ LF line end/ utf-8 encoding/ insert final new line)
3. Reduce warning noise during middle steps by `dotnet build Everlong.Nester.slnx -v:q --nologo -clp:ErrorsOnly` or a chained `tail` command.
4. Never run `dotnet format` solution-wide: it rewrites files you never touched and buries the diff.

## Commit Gate

1. Ensure changed files are formatted with `dotnet format` and free of build warnings/diagnostics, specifically `IDE0005`, `CS1574`, and `CS0105`.
2. Format the commit message according to the `commit-messages` skill.
