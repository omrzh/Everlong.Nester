#!/usr/bin/env python3
"""Rewrap a commit message file: subject untouched, body paragraphs wrapped.

The gate counts characters and hand-wrapping drifts, so this keeps it honest.

    python .agents/py/rewrap.py artifacts/msg.txt [--width 70]
"""
import argparse, sys, textwrap
from pathlib import Path

# The console this runs in is UTF-8; Python would otherwise pick the
# platform codepage and mangle non-ASCII output (paths, em dashes).
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("path", help="the commit message file to rewrite in place")
    ap.add_argument("-w", "--width", type=int, default=70,
                    help="body wrap column (default: 70, the gate allows 72)")
    args = ap.parse_args()

    path = Path(args.path)
    if not path.is_file():
        print(f"rewrap: {path} is not a file", file=sys.stderr)
        return 2

    lines = path.read_text(encoding="utf-8").splitlines()
    if not lines:
        print(f"rewrap: {path} is empty", file=sys.stderr)
        return 2

    subject, body = lines[0], lines[1:]
    paras, cur = [], []
    for line in body:
        if line.strip():
            cur.append(line.strip())
        elif cur:
            paras.append(" ".join(cur)); cur = []
    if cur:
        paras.append(" ".join(cur))

    out = [subject]
    for para in paras:
        out.append("")
        out.extend(textwrap.wrap(para, width=args.width,
                                 break_long_words=False, break_on_hyphens=False))
    path.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")

    longest = max((len(l) for l in out[1:]), default=0)
    print(f"rewrapped: {len(out)} lines, longest body line {longest}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
