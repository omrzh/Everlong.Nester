#!/usr/bin/env python3
"""Rewrite the platform alias spellings under a root to their P-prefixed form.

Only code is rewritten; inside comments only `cref="..."` targets are, so prose
such as "the Visual Tree" or "Thickness of the glass" stays readable.

The rename is blunt on purpose: it cannot tell a type position from a member
position, so a member that shares an alias' name (an inherited property, a
property of our own) is renamed too and has to be corrected by hand — the
compiler names those spots.  Run it with `--dry-run` first.

    python .agents/py/rename_aliases.py --root src/Everlong.Nester.Wpf --platform wpf [--dry-run]
"""
import argparse, os, re, sys
from pathlib import Path

# The console this runs in is UTF-8; Python would otherwise pick the
# platform codepage and mangle non-ASCII output (paths, em dashes).
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

MAP = [
    ("PlatformApp", "PApp"),
    ("PlatformCanvas", "PCanvas"),
    ("PlatformControl", "PControl"),
    ("PlatformGrid", "PGrid"),
    ("PlatformWindow", "PWindow"),
    ("Color", "PColor"),
    ("ContentControl", "PContentControl"),
    ("HorizontalAlignment", "PHorizontalAlignment"),
    ("Rect", "PRect"),
    ("VerticalAlignment", "PVerticalAlignment"),
    ("Visual", "PVisual"),
    ("WindowState", "PWindowState"),
]
WPF_ONLY = [("CornerRadius", "PCornerRadius"), ("Thickness", "PThickness")]

# Longest first so PlatformX is handled before the plain aliases; the
# lookbehind keeps `Avalonia.Media.Color` and `SolidColorBrush` untouched.
MAP.sort(key=lambda p: -len(p[0]))
CREF = re.compile(r'cref="[^"]*"')


def rename(text: str, mapping) -> str:
    out = text
    for old, new in mapping:
        out = re.sub(r"(?<![\w.])" + old + r"\b", new, out)
    return out


def split_comment(line: str):
    """Return (code, comment) — the comment starts at the first `//` outside a
    string literal, so `"https://nester.dev"` stays in the code part."""
    i, quote, verbatim = 0, False, False
    while i < len(line):
        c = line[i]
        if quote:
            if verbatim:
                if c == '"':
                    quote = False
            elif c == "\\":
                i += 2
                continue
            elif c == '"':
                quote = False
        elif c == "@" and i + 1 < len(line) and line[i + 1] == '"':
            quote, verbatim = True, True
            i += 1
        elif c == '"':
            quote, verbatim = True, False
        elif c == "'":
            i += 1
            while i < len(line) and line[i] != "'":
                i += 2 if line[i] == "\\" else 1
        elif c == "/" and i + 1 < len(line) and line[i + 1] == "/":
            return line[:i], line[i:]
        i += 1
    return line, ""


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", required=True, help="directory to rewrite, e.g. src/Everlong.Nester.Wpf")
    ap.add_argument("--platform", choices=("avalonia", "wpf"), default="avalonia",
                    help="wpf adds the two geometry aliases (default: avalonia)")
    ap.add_argument("--dry-run", action="store_true", help="list what would change, write nothing")
    args = ap.parse_args()

    root = Path(args.root)
    if not root.is_dir():
        print(f"rename_aliases: {root} is not a directory", file=sys.stderr)
        return 2

    mapping = MAP + WPF_ONLY if args.platform == "wpf" else MAP
    touched = 0
    for dirpath, dirnames, names in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in ("obj", "bin")]
        for name in names:
            if not name.endswith(".cs") or name.startswith("GlobalUsings."):
                continue
            path = Path(dirpath) / name
            original = path.read_text(encoding="utf-8-sig")
            out = "".join(
                rename(code, mapping) + CREF.sub(lambda m: rename(m.group(0), mapping), comment)
                for code, comment in (split_comment(line) for line in original.splitlines(keepends=True)))
            if out == original:
                continue
            touched += 1
            print(f"{'would touch' if args.dry_run else 'touched'} {path}")
            if not args.dry_run:
                path.write_text(out, encoding="utf-8", newline="")
    print(f"{'would touch' if args.dry_run else 'touched'} {touched} file(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
