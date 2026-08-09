#!/usr/bin/env python3
"""Smoke test for the packed project templates.

`_build/` packs `Everlong.Nester.Templates`, and nothing instantiates it: a
template's only proof is a `dotnet new` run, and one that packs but generates a
project that does not build fails on the consumer's machine.  This installs the
packed package into the template store, generates one project per template,
restores and builds each, and uninstalls again.

    python .agents/py/template_smoke.py [--package <nupkg>] [--keep] [--repo <path>]

The generated projects go to a fresh directory under the system temp, never
under this repository: `Directory.Packages.props` and `Directory.Build.props`
are inherited by everything below the root, and a generated project is a
consumer outside the tree — literal package versions, central package
management of its own.  `--keep` leaves the trees behind for inspection; a run also sweeps whatever
an earlier one could not delete, which is what a project held open by an IDE
leaves behind.

`Everlong.Nester` is not on nuget.org, so the run writes a `nuget.config` into
the work directory naming this repository's `artifacts/` folder as an extra
source; nuget.org is inherited from the machine's own config, and nothing is
written outside the work directory.

Exit: 0 every step passed, 1 a step failed, 2 a precondition or usage problem.
"""
import argparse, shutil, subprocess, sys, tempfile, time
import xml.sax.saxutils as saxutils
from pathlib import Path

# The console this runs in is UTF-8; Python would otherwise pick the
# platform codepage and mangle non-ASCII output.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

PACKAGE_ID = "Everlong.Nester.Templates"
WORK_PREFIX = "nester-template-smoke-"

# (short name, extra `dotnet new` arguments, the framework that results).
# The third case is the template's own `Framework` symbol: it rewrites the
# project file's TFM, so a template whose `replaces` drifted would build for
# the default framework and nothing else.
CASES = [
    ("nester-avalonia", [], "net10.0"),
    ("nester-wpf", [], "net10.0-windows"),
    ("nester-avalonia", ["-F", "net8.0"], "net8.0"),
]


def run(cmd, cwd=None):
    proc = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True,
                          encoding="utf-8", errors="replace")
    return proc.returncode, (proc.stdout or "") + (proc.stderr or "")


def tail(text, lines=25):
    kept = [line for line in text.splitlines() if line.strip()]
    return "\n".join(kept[-lines:])


def uninstall():
    """Uninstalling removes one template entry per pass; loop while it does."""
    for _ in range(4):
        code, out = run(["dotnet", "new", "uninstall", PACKAGE_ID])
        if code != 0:
            return
        lowered = out.lower()
        if not any(word in lowered for word in
                   ("success", "uninstalled", "成功", "已卸载")):
            return


def remove_tree(path: Path, attempts=3, delay=1.0) -> bool:
    """Delete a work directory, waiting out the handle on a just-built project."""
    for _ in range(attempts):
        shutil.rmtree(path, ignore_errors=True)
        if not path.exists():
            return True
        time.sleep(delay)
    return not path.exists()


def sweep_stale(min_age=900):
    """Remove the work directories earlier runs could not delete.

    A generated project stays held open for a while after its build — an IDE's
    project-system build host reads every new project file — so the cleanup at
    the end of a run is best-effort, and this is what actually removes them.
    Directories younger than `min_age` are left alone, so `--keep` output is
    not deleted while someone is reading it.
    """
    now = time.time()
    swept = 0
    for stale in Path(tempfile.gettempdir()).glob(f"{WORK_PREFIX}*"):
        try:
            if now - stale.stat().st_mtime < min_age:
                continue
        except OSError:
            continue
        if remove_tree(stale, attempts=2, delay=0.5):
            swept += 1
    if swept:
        print(f"swept {swept} work director{'y' if swept == 1 else 'ies'} from an earlier run")


def find_package(repo: Path, explicit: str) -> Path:
    if explicit:
        path = Path(explicit).resolve()
        if not path.is_file():
            sys.exit(f"no such package: {path}")
        return path

    artifacts = repo / "artifacts"
    found = sorted(artifacts.glob(f"{PACKAGE_ID}.*.nupkg"),
                   key=lambda p: p.stat().st_mtime)
    if not found:
        sys.exit(f"no {PACKAGE_ID} package in {artifacts} — run build.cmd first")
    return found[-1]


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", default=str(Path(__file__).resolve().parents[2]),
                    help="repository root (default: this script's repository)")
    ap.add_argument("--package",
                    help="the template package to smoke (default: the newest in artifacts/)")
    ap.add_argument("--keep", action="store_true",
                    help="keep the generated projects for inspection")
    args = ap.parse_args()

    if shutil.which("dotnet") is None:
        sys.exit("dotnet is not on PATH")

    repo = Path(args.repo).resolve()
    package = find_package(repo, args.package)
    artifacts = repo / "artifacts"
    work = Path(tempfile.mkdtemp(prefix=WORK_PREFIX))
    results = []

    def step(name, cmd):
        code, out = run(cmd)
        ok = code == 0
        results.append((name, ok))
        print(f"{'PASS' if ok else 'FAIL'}  {name}", flush=True)
        if not ok:
            print(tail(out), file=sys.stderr)
        return ok

    print(f"package: {package}")
    print(f"work:    {work}")

    # The artifacts folder is added as a source by config file rather than by a
    # `--source` argument: an MSYS shell hands a URL in argv to dotnet mangled
    # into a relative path (`https:\api.nuget.org\...`), and the failure reads
    # as a missing local source.
    (work / "nuget.config").write_text(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        "<configuration>\n"
        "  <packageSources>\n"
        f"    <add key=\"nester-artifacts\" value=\"{saxutils.escape(str(artifacts))}\" />\n"
        "  </packageSources>\n"
        "</configuration>\n",
        encoding="utf-8")

    uninstall()
    sweep_stale()
    try:
        if not step(f"install {package.name}",
                    ["dotnet", "new", "install", str(package)]):
            return 1

        for index, (short, extra, framework) in enumerate(CASES):
            label = f"{short} {framework}"
            if short == "nester-wpf" and sys.platform != "win32":
                results.append((f"new {label} (skipped: WPF is Windows-only)", True))
                continue

            name = f"Smoke{index}"
            target = work / name
            if not step(f"new {label}",
                        ["dotnet", "new", short, "-n", name, "-o", str(target), *extra]):
                continue

            project = next(iter(target.glob("*.csproj")), None)
            if project is None:
                results.append((f"new {label}: generated no csproj", False))
                continue

            if not step(f"restore {label}",
                        ["dotnet", "restore", str(project)]):
                continue

            step(f"build {label}",
                 ["dotnet", "build", str(project), "--no-restore", "-v:m", "--nologo"])
    finally:
        uninstall()
        if args.keep:
            print(f"kept: {work}")
        elif not remove_tree(work):
            print(f"left for the next run to sweep: {work}")

    failed = [name for name, ok in results if not ok]
    print()
    if failed:
        print(f"{len(failed)} step(s) failed:")
        for name in failed:
            print(f"  - {name}")
        return 1
    print(f"all {len(results)} step(s) passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
