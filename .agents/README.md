# .agents/

This repository's local agent workspace. Three kinds of thing live here:

| Path | What it is |
|---|---|
| `commit-gate.sh` | the commit gate — `--help` prints the whole story; it is the one check that runs before a commit |
| `py/` | analysis and rewriting helpers; each one documents itself (`python py/<tool>.py --help`) |
| `handoff.md` | session handoff notes — untracked, ignored by git, deleted once absorbed |

## Tools

| Tool | Use it for |
|---|---|
| `py/rewrap.py` | rewrap a commit message body to the gate's column limit |
| `py/dep.py` | declaration-dependency layers of a project directory |
| `py/shared_refs.py` | which platform halves a single-source file needs before it can be linked |
| `py/rename_aliases.py` | rewrite platform alias spellings to the `P` prefix, comments left readable |

## Conventions

- **Never write inside `.git`.** The tools touch the working tree and
  `artifacts/` only.
- Every script takes its paths as arguments or derives them from its own
  location — none of them assumes this machine's layout.
- Exit codes: `0` fine, `1` a problem was found, `2` bad usage.
- `artifacts/` holds outputs and throwaway state; this folder holds programs
  that can be run again tomorrow.
