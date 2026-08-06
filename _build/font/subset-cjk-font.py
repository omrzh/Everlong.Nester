#!/usr/bin/env python3
"""Rebuild the examples' embedded CJK font as a coverage-limited subset.

`examples/Template.Avalonia/Assets/Fonts/NotoSansSC-Regular.otf` is embedded for the Browser and
Android hosts, which have no system CJK font to fall back on.  Nothing else consumes it: the
desktop app and the template package both resolve Chinese through the platform.  So the font is
cut down to what is actually rendered — which takes it from 16.05 MB (80.6% of every clone) to
about a third of a megabyte:

  * every codepoint the example sources use inside the font's territory, and
  * ASCII, Latin-1, the general-punctuation, CJK-punctuation and fullwidth blocks, carried whole.

The territory is the blocks this font is the answer for: ASCII, Latin-1, general punctuation, CJK
punctuation, kana, CJK Unified Ideographs and their Ext A block, and the fullwidth forms.  Anything
outside it — the tour's emoji and symbol glyphs — is left to the platform's emoji fallback, which
the desktop host configures explicitly.

`VerifyFontCoverage` (Nuke) fails when an edit adds a codepoint the shipped font does not map, so
this script and that gate move together: run this, then the gate.

    uv run --with fonttools python _build/font/subset-cjk-font.py

fontTools is not a dependency of this repository — `uv` runs it in a throwaway environment.

A re-run derives the subset from itself, which is idempotent while no new codepoint appears.  When
the gate reports one and the committed subset cannot supply it, fetch the pristine font — Noto
CJK's own SC subset, `Sans/SubsetOTF/SC/NotoSansSC-Regular.otf` from `notofonts/noto-cjk`, or the
full OTF from Google Fonts — and pass it as `--source`.
"""

import argparse
import os
import sys

from fontTools import subset

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FONT = os.path.join(REPO, "examples", "Template.Avalonia", "Assets", "Fonts", "NotoSansSC-Regular.otf")

# The sources whose text the embedded font has to render — the same set `VerifyFontCoverage`
# scans.  `.axaml`/`.xaml` are the views, `.jsonc`/`.json` the locale strings the generator reads.
ROOTS = [os.path.join(REPO, "examples")]
EXTENSIONS = (".cs", ".axaml", ".xaml", ".jsonc", ".json")
SKIP_DIRECTORIES = {"bin", "obj"}

# Carried whole: small blocks whose every codepoint can turn up in text a reader adds next to the
# tour, so re-subsetting for one more punctuation mark would be a waste of a maintainer's time.
CARRIED_WHOLE = [(0x20, 0x7E), (0xA0, 0xFF), (0x2000, 0x206F), (0x3000, 0x303F), (0xFF00, 0xFFEF)]

# The blocks this font is the answer for.  A used codepoint inside them is required coverage; one
# outside them is the platform's (see the module docstring).
TERRITORY = CARRIED_WHOLE + [(0x3040, 0x30FF), (0x3400, 0x4DBF), (0x4E00, 0x9FFF)]


def used_codepoints():
    cps = set()
    for root in ROOTS:
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_DIRECTORIES]
            for name in filenames:
                if not name.endswith(EXTENSIONS):
                    continue
                try:
                    text = open(os.path.join(dirpath, name), encoding="utf-8").read()
                except (UnicodeDecodeError, OSError):
                    continue
                cps.update(ord(ch) for ch in text)
    return cps


def in_territory(codepoints):
    return {c for c in codepoints if any(lo <= c <= hi for lo, hi in TERRITORY)}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--source", default=FONT,
                        help="font to derive the subset from (default: the committed subset itself)")
    args = parser.parse_args()

    used = {c for c in used_codepoints() if c >= 0x80}
    required = in_territory(used)

    requested = set()
    for lo, hi in CARRIED_WHOLE:
        requested.update(range(lo, hi + 1))
    requested |= required

    options = subset.Options()
    options.hinting = False
    options.glyph_names = False
    options.legacy_cmap = False
    options.layout_features = ["*"]
    options.notdef_outline = True
    options.name_IDs = ["*"]
    options.name_legacy = False
    options.name_languages = ["*"]
    options.drop_tables += ["DSIG"]
    options.recalc_timestamp = False

    source_size = os.path.getsize(args.source)
    font = subset.load_font(args.source, options)
    mapped = set(font.getBestCmap().keys())

    # Required coverage the source cannot supply: the gate will reject these, so the run does too
    # rather than writing a subset that is already known to be short.
    missing = sorted(required - mapped)
    # Outside the territory and unmapped: emoji and symbol glyphs the platform renders.
    delegated = sorted(c for c in used - required if c not in mapped)

    if missing:
        print(f"{args.source} does not map {len(missing)} required codepoint(s):", file=sys.stderr)
        for cp in missing:
            print(f"  U+{cp:04X}  {chr(cp)!r}", file=sys.stderr)
        print("fetch the pristine font and re-run with --source <path>", file=sys.stderr)

    subsetter = subset.Subsetter(options=options)
    subsetter.populate(unicodes=requested & mapped)
    subsetter.subset(font)
    subset.save_font(font, FONT, options)
    font.close()

    size = os.path.getsize(FONT)
    print(f"required {len(required)} (+{len(requested) - len(required)} carried whole), "
          f"{len(missing)} unmappable")
    print(f"{source_size:,} bytes -> {size:,} bytes ({size / 1048576:.2f} MB)")
    if delegated:
        print(f"{len(delegated)} codepoint(s) outside the font's territory are left to the platform: "
              + " ".join(f"U+{c:04X}" for c in delegated))
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
