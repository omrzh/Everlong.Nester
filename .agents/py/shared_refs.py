#!/usr/bin/env python3
"""Reference map around the single-source (shared) files.

The platform packages compile one body of source twice: a set of files kept in
the Avalonia project is linked into the WPF project by compile-include, so a
shared file may only be linked once every name it spells already exists on the
WPF side too.  This prints, per shared file, which WPF-only files and which
other shared files it names.

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

SHARED = [
    "DI/ServiceCollectionExtensions.cs",
    "Controls/StagePanel.cs", "Controls/NavigationHost.cs", "Controls/ContentLayerLease.cs",
    "Controls/TreeControl.shared.cs", "Controls/TreeItem.shared.cs",
    "Routing/Router.cs", "Routing/PlatformStackModel.cs", "Routing/PlatformStages.cs",
    "Presentation/SharedAttributes.cs", "Presentation/ShellFlyingLayer.cs",
    "Presentation/ISceneTransition.cs", "Presentation/TransitionKind.cs",
    "Presentation/TransitionContext.cs", "Presentation/ILayoutControl.cs",
]
# The Avalonia-only halves of the platform-paired types: a shared file naming
# one of these names can mean either half, so a hit is not evidence.
AV_PAIRED = [
    "Controls/ContentLayer.cs", "Controls/LayoutBody.cs", "Controls/TreeControl.cs",
    "Controls/TreeItem.cs", "Presentation/IViewLocator.cs", "Presentation/ViewLocatorBase.cs",
    "Presentation/CompositeViewLocator.cs", "Presentation/TransitionContext.avalonia.cs",
    "Shell/AvaloniaShell.cs", "Shell/AvaloniaShell.Platform.cs", "Shell/AvaloniaShellViews.cs",
]
DECL = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|private|protected|file)?\s*'
    r'(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|ref\s+)*'
    r'(class|interface|record(?:\s+(?:struct|class))?|struct|enum|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)',
    re.M)


def blank(text: str) -> str:
    text = re.sub(r'/\*.*?\*/', ' ', text, flags=re.S)
    return re.sub(r'^\s*//.*$', ' ', text, flags=re.M)


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

    wpf_files = []
    for dirpath, dirnames, names in os.walk(wpf):
        dirnames[:] = [d for d in dirnames if d not in ("obj", "bin")]
        wpf_files += [os.path.relpath(os.path.join(dirpath, n), wpf).replace("\\", "/")
                      for n in names if n.endswith(".cs")]

    wpf_decl = decls(wpf_files, wpf)
    shared_decl = decls(SHARED, av)
    paired = set(decls(AV_PAIRED, av))
    texts = {s: blank((av / s).read_text(encoding="utf-8-sig"))
             for s in SHARED if (av / s).exists()}

    for name in SHARED:
        text = texts.get(name)
        if text is None:
            print(f"{name}\n   MISSING under {av}")
            continue
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
