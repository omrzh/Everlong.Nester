#!/usr/bin/env python3
"""Print the declaration-dependency layers of a project directory.

Level 1 holds the files with no dependency inside the directory, level 2 the
ones that only reach into level 1, and so on: a level only ever reaches
downward.  The cross-folder edges printed after the levels are the same
information read by folder.

    python .agents/py/dep.py --root src/Everlong.Nester.Wpf
"""
import argparse, collections, os, re, sys
from pathlib import Path

# The console this runs in is UTF-8; Python would otherwise pick the
# platform codepage and mangle non-ASCII output (paths, em dashes).
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

DECL = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|private|protected|file)?\s*'
    r'(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|ref\s+)*'
    r'(class|interface|record|struct|enum|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)',
    re.M)


def blank_comments(text: str) -> str:
    text = re.sub(r'/\*.*?\*/', ' ', text, flags=re.S)
    return re.sub(r'^\s*//.*$', ' ', text, flags=re.M)


def load(root: Path) -> dict:
    files = {}
    for dirpath, _, names in os.walk(root):
        if any(p in dirpath for p in (os.sep + 'obj', os.sep + 'bin')):
            continue
        for name in names:
            if not name.endswith('.cs'):
                continue
            path = os.path.join(dirpath, name)
            files[path] = (blank_comments(open(path, encoding='utf-8-sig').read()), None)
    return files


def decls(files: dict) -> dict:
    out = collections.defaultdict(list)
    for path, (text, _) in files.items():
        for _, name in DECL.findall(text):
            out[name].append(path)
    return out


def rel(path: str, root: Path) -> str:
    return os.path.relpath(path, root).replace('\\', '/')


def layers(files: dict, root: Path, label: str) -> None:
    declared = decls(files)
    deps = {}
    for path, (text, _) in files.items():
        mine = {n for n, ps in declared.items() if path in ps}
        found = set()
        for name, ps in declared.items():
            if name in mine:
                continue
            if re.search(r'\b' + re.escape(name) + r'\b', text):
                found.update(q for q in ps if q != path)
        deps[path] = found

    # Peel the leaves off one round at a time; a level only reaches downward.
    remaining = dict(deps)
    level, n = {}, 0
    while remaining:
        leaf = [p for p, s in remaining.items() if not (s & set(remaining))]
        if not leaf:
            leaf = sorted(remaining, key=lambda x: len(remaining[x] & set(remaining)))[:1]
        n += 1
        for p in leaf:
            level[p] = n
            remaining.pop(p)

    print(f"===== {label}: {len(files)} .cs files, {n} levels =====")
    by_level = collections.defaultdict(list)
    for path, lvl in level.items():
        by_level[lvl].append(rel(path, root))
    for lvl in sorted(by_level):
        print(f"\n-- L{lvl} ({len(by_level[lvl])}) --")
        for name in sorted(by_level[lvl]):
            print(f"  {name}")

    print(f"\n----- {label} cross-folder edges -----")
    edges = collections.Counter()
    for path, s in deps.items():
        a = rel(path, root).split('/')[0]
        for q in s:
            b = rel(q, root).split('/')[0]
            if a != b:
                edges[(a, b)] += 1
    for (a, b), count in sorted(edges.items()):
        print(f"  {a} -> {b}: {count}")
    if not edges:
        print("  (none)")


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", required=True,
                    help="project directory to analyse, e.g. src/Everlong.Nester.Wpf")
    args = ap.parse_args()

    root = Path(args.root)
    if not root.is_dir():
        print(f"dep: {root} is not a directory", file=sys.stderr)
        return 2
    layers(load(root), root, root.name)
    return 0


if __name__ == "__main__":
    sys.exit(main())
