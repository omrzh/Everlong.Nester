# .agents/

This repository's local agent workspace:

| Path | What it is |
|---|---|
| `commit-gate.sh` | the commit gate — `--help` prints the whole story; it is the one check that runs before a commit |
| `py/` | analysis and rewriting helpers; each one documents itself (`python py/<tool>.py --help`) |
| `tmp/` | the scratch directory — throwaway files, staged work and the session tools; ignored by git, never committed |
| `handoff.md` | session handoff notes — untracked, ignored by git, deleted once absorbed |

## Tools

| Tool | Use it for |
|---|---|
| `py/rewrap.py` | rewrap a commit message body to the gate's column limit |
| `py/dep.py` | declaration-dependency layers of a project directory |
| `py/shared_refs.py` | which platform halves a single-source file needs before it can be linked |
| `py/rename_aliases.py` | rewrite platform alias spellings to the `P` prefix, comments left readable |

## Conventions

- **Never write inside `.git`.** The tools touch the working tree and `tmp/`
  only.
- Every script takes its paths as arguments or derives them from its own
  location — none of them assumes this machine's layout.
- A script the docs tell you to run as `./name` is stored executable: the index
  mode is what git hands out, not the working copy's
  (`git update-index --chmod=+x <path>`).
- Exit codes: `0` fine, `1` a problem was found, `2` bad usage.
- Scratch goes in `tmp/`, never in `artifacts/`: `artifacts/` is reserved for
  build output, and the build cleans it without asking. This folder holds the
  programs that can be run again tomorrow; `tmp/` holds what a run produces.
