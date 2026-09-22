#!/usr/bin/env python3
"""Reference map around the single-source (shared) files.

The platform packages compile one body of source twice: a set of files kept in
the Avalonia project is linked into the WPF project by compile-include, so a
shared file may only be linked once every name it spells already exists on the
WPF side too.  This prints, per shared file, which WPF-only files and which
other shared files it names.

Both sets are derived rather than kept by hand: the shared set is the WPF
project's compile-include list, and a name is "paired" when an Avalonia-only
file and a WPF-only file both declare it.  A hand-kept copy of either went
stale the moment a file was renamed or moved.

    python .agents/py/shared_refs.py [--repo <path>]
"""
import argparse, collections, os, re, sys
from pathlib import Path

# The console this runs in is UTF-8; Python would otherwise pick the
# platform codepage and mangle non-ASCII output (paths, em dashes).
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

# The WPF project is the authority on which Avalonia files it links; a shared
# file it names but cannot find is a build break, not a silent miss.
LINK = re.compile(
    r'<Compile\s+Include="\.\.\\Everlong\.Nester\.Avalonia\\([^"]+)"'
    r'\s+Link="[^"]+"')
DECL = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|private|protected|file)?\s*'
    r'(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|ref\s+)*'
    r'(class|interface|record(?:\s+(?:struct|class))?|struct|enum|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)',
    re.M)


def blank(text: str) -> str:
    text = re.sub(r'/\*.*?\*/', ' ', text, flags=re.S)
    return re.sub(r'^\s*//.*$', ' ', text, flags=re.M)


def cs_files(root: Path) -> list:
    out = []
    for dirpath, dirnames, names in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in ("obj", "bin")]
        out += [os.path.relpath(os.path.join(dirpath, n), root).replace("\\", "/")
                for n in names if n.endswith(".cs")]
    return out


def decls(paths, root: Path) -> dict:
    out = collections.defaultdict(list)
    for name in paths:
        full = root / name
        if not full.exists():
            continue
        for _, declared in DECL.findall(blank(full.read_text(encoding="utf-8-sig"))):
            out[declared].append(name)
    return out


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", default=str(Path(__file__).resolve().parents[2]),
                    help="repository root (default: this script's repository)")
    args = ap.parse_args()

    repo = Path(args.repo).resolve()
    wpf = repo / "src/Everlong.Nester.Wpf"
    av = repo / "src/Everlong.Nester.Avalonia"
    if not wpf.is_dir() or not av.is_dir():
        print(f"shared_refs: {wpf} and {av} must both exist", file=sys.stderr)
        return 2

    csproj = wpf / "Everlong.Nester.Wpf.csproj"
    shared = [p.replace("\\", "/") for p in LINK.findall(csproj.read_text(encoding="utf-8-sig"))]
    absent = [s for s in shared if not (av / s).exists()]
    if absent:
        print(f"shared_refs: linked by WPF but absent under {av}: {', '.join(absent)}",
              file=sys.stderr)
        return 2

    wpf_decl = decls(cs_files(wpf), wpf)
    shared_decl = decls(shared, av)
    # A name declared on both sides is the paired halves of one type: a shared
    # file naming it may mean the Avalonia half, so the hit is not evidence of
    # a WPF-only dependency.
    paired = set(decls([f for f in cs_files(av) if f not in shared], av)) & set(wpf_decl)
    texts = {s: blank((av / s).read_text(encoding="utf-8-sig")) for s in shared}

    for name in shared:
        text = texts[name]
        own = {n for n, ps in shared_decl.items() if name in ps}
        wpf_hits, shared_hits = set(), set()
        for declared, files in wpf_decl.items():
            if declared in own or declared in paired:
                continue
            if re.search(r"\b" + re.escape(declared) + r"\b", text):
                wpf_hits.update(files)
        for declared, files in shared_decl.items():
            if declared in own or declared in paired:
                continue
            if re.search(r"\b" + re.escape(declared) + r"\b", text):
                shared_hits.update(f for f in files if f != name)
        print(name)
        print(f"   wpf-only : {', '.join(sorted(wpf_hits)) or '-'}")
        print(f"   shared   : {', '.join(sorted(shared_hits)) or '-'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
