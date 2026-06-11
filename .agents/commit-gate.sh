#!/usr/bin/env bash
#
# The commit gate — the `AGENTS.md` → "Commit Gate" section, executable.
#
# Policy lives in .editorconfig, which is where the toolchain reads it: which
# rules exist, and how loud they are.  This script is the procedure that makes
# them bite.  There is deliberately no list of diagnostic IDs here — such a
# list could only mean anything by overriding .editorconfig's severity filter,
# and an override is exactly how a gate ends up green on a rule that nobody
# switched on.
#
#   build    `--no-incremental` and NO `-clp:ErrorsOnly` (#3): the quiet build
#            filters warnings.  Refuses any warning or error — this is where
#            the compiler and analyzer IDs (CS*, MVVMTK*) surface.  It does NOT
#            see the option-backed style rules, at any severity.
#   format   one project at a time, never solution-wide (HARD RULE 4), at the
#            default severity: the option-backed IDE rules (coalesce, null
#            propagation) are visible only here.  Paths must arrive relative to
#            the REPOSITORY ROOT — an MSYS absolute path (/d/...) or a
#            project-relative `--include` makes it analyse nothing and exit 0.
#   test     the MTP runner accepts `-v` and nothing else (#1), and a run that
#            reports zero tests is a failure, not a pass.
#   message  subject and body shape per the `commit-messages` skill.
#
# Read-only: it never formats in place, never stages, never commits.  Logs land
# in artifacts/commit-gate/.  `--self-test` plants one violation per half and
# proves both still refuse it — including that the rule behind it is enabled.
#
# Usage: .agents/commit-gate.sh [options]
#
#   --message <file>   also check a commit message file (`-` reads stdin)
#   --scoped           check formatting only for the paths this branch changed
#   --no-build         skip the build step
#   --no-format        skip the format step
#   --no-test          skip the test step
#   --quiet            print only the failing steps' output
#   --self-test        prove both halves refuse a planted violation, then exit
#   -h, --help         print this block
#
# Exit: 0 the gate passed · 1 the gate failed · 2 bad usage.

set -uo pipefail

# Conventional-commit types this repository uses (see `git log --oneline`).
COMMIT_TYPES=(feat fix refactor docs test build chore perf style ci revert)
SUBJECT_MAX=72
BODY_MAX=72

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION="Everlong.Nester.slnx"
LOG_DIR="$REPO_ROOT/artifacts/commit-gate"

DO_BUILD=1 DO_FORMAT=1 DO_TEST=1 QUIET=0 SCOPED=0 SELF_TEST=0 MESSAGE=""

while [ $# -gt 0 ]; do
  case "$1" in
    --message)   MESSAGE="${2:-}"; shift 2 ;;
    --scoped)    SCOPED=1; shift ;;
    --no-build)  DO_BUILD=0; shift ;;
    --no-format) DO_FORMAT=0; shift ;;
    --no-test)   DO_TEST=0; shift ;;
    --quiet)     QUIET=1; shift ;;
    --self-test) SELF_TEST=1; shift ;;
    -h|--help)   awk 'NR>2 && /^#/ {sub(/^# ?/, ""); print; next} NR>2 {exit}' "${BASH_SOURCE[0]}"; exit 0 ;;
    *)           echo "commit-gate: unknown option '$1' (try --help)" >&2; exit 2 ;;
  esac
done

cd "$REPO_ROOT" || exit 2

if [ -t 1 ]; then C_RED=$'\033[31m'; C_GREEN=$'\033[32m'; C_DIM=$'\033[2m'; C_OFF=$'\033[0m'
else C_RED= C_GREEN= C_DIM= C_OFF=; fi

STEP_NAMES=() STEP_VERDICTS=() STEP_NOTES=()
FAILED=0

head_line() { printf '\n%s==> %s%s\n' "$C_DIM" "$1" "$C_OFF"; }
ok()   { printf '%s  PASS%s %s%s\n' "$C_GREEN" "$C_OFF" "$1" "${2:+ ($2)}"; }
bad()  { printf '%s  FAIL%s %s%s\n' "$C_RED" "$C_OFF" "$1" "${2:+ ($2)}"; }
note() { printf '%s         %s%s\n' "$C_DIM" "$1" "$C_OFF"; }
record() { STEP_NAMES+=("$1"); STEP_VERDICTS+=("$2"); STEP_NOTES+=("$3"); }
# The file:line of every diagnostic in a log, deduplicated and stripped of the
# project suffix MSBuild appends.
diagnostics() { grep -E ": (warning|error) " "$1" | sed 's/ \[.*//' | sort -u; }

mkdir -p "$LOG_DIR"

mapfile -t PROJECTS < <(grep -o 'Project Path="[^"]*"' "$SOLUTION" | sed 's/Project Path="//; s/"$//')

# ---------------------------------------------------------------- self-test --
if [ "$SELF_TEST" = 1 ]; then
  head_line "self-test (both halves must refuse a planted violation)"
  # Relative paths on purpose: an MSYS absolute path is a no-op for dotnet format.
  PROBE="src/Everlong.Nester/ZzGateProbe.cs"
  PROBE_PROJ="src/Everlong.Nester/Everlong.Nester.csproj"
  cleanup() { rm -f "$PROBE"; }
  trap cleanup EXIT INT TERM
  cat >"$PROBE" <<'EOF'
using System.Text;

namespace Everlong.Nester;

internal static class ZzGateProbe
{
  public static string Pick(string? x, string fallback) => x != null ? x : fallback;
}
EOF
  SELF_FAILED=0

  # An unused using is a warning the build reports.
  dotnet build "$PROBE_PROJ" --no-incremental -v:q --nologo >"$LOG_DIR/self-test-build.log" 2>&1
  if diagnostics "$LOG_DIR/self-test-build.log" | grep -q 'IDE0005'; then
    ok "build half refuses an unused using"
  else
    bad "build half did NOT report IDE0005 — is dotnet_diagnostic.IDE0005.severity set?"
    SELF_FAILED=1
  fi

  # A null check is an option-backed rule only `dotnet format` can see, so this
  # check also proves the rule is enabled in .editorconfig above `suggestion`.
  dotnet format "$PROBE_PROJ" --verify-no-changes >"$LOG_DIR/self-test-format.log" 2>&1
  if diagnostics "$LOG_DIR/self-test-format.log" | grep -q 'IDE0029'; then
    ok "format half refuses a null-check shape"
  else
    bad "format half did NOT report IDE0029 — is dotnet_style_coalesce_expression at warning?"
    SELF_FAILED=1
  fi

  # The --scoped form narrows with --include, whose paths are repository
  # relative; a wrong shape here checks nothing and still passes.
  dotnet format "$PROBE_PROJ" --verify-no-changes --include "$PROBE" \
    >"$LOG_DIR/self-test-scoped.log" 2>&1
  if [ "$(diagnostics "$LOG_DIR/self-test-scoped.log" | wc -l | tr -d ' ')" -gt 0 ]; then
    ok "--scoped form refuses the changed file"
  else
    bad "--scoped form did NOT see a changed file (--include is repository relative)"
    SELF_FAILED=1
  fi

  cleanup; trap - EXIT INT TERM
  [ "$SELF_FAILED" = 0 ] && exit 0
  exit 1
fi

# ---------------------------------------------------------------- preflight --
head_line "preflight"
if ! command -v dotnet >/dev/null 2>&1; then
  bad "dotnet not on PATH"; exit 2
fi
if [ ! -f "$SOLUTION" ]; then
  bad "$SOLUTION not found under $REPO_ROOT"; exit 2
fi
note "${#PROJECTS[@]} projects; policy in .editorconfig, procedure here"
note "$(git status --porcelain | wc -l | tr -d ' ') pending path(s)"
if git diff --cached --name-only --diff-filter=ACM | grep -qx '.agents/handoff.md'; then
  note "WARNING: .agents/handoff.md is staged — it is a temporary file and must never be committed"
fi

# -------------------------------------------------------------------- build --
if [ "$DO_BUILD" = 1 ]; then
  head_line "build (no -clp:ErrorsOnly, --no-incremental)"
  LOG="$LOG_DIR/build.log"
  dotnet build "$SOLUTION" --no-incremental -v:q --nologo >"$LOG" 2>&1
  CODE=$?
  ERRORS="$(diagnostics "$LOG" | grep ': error ' || true)"
  WARNS="$(diagnostics "$LOG" | grep ': warning ' || true)"
  if [ "$CODE" -ne 0 ]; then
    bad "build failed (exit $CODE)"
    printf '%s\n' "$ERRORS" | head -n 20 | sed 's/^/         /'
    note "log: ${LOG#"$REPO_ROOT"/}"
    record build fail "exit $CODE"; FAILED=1
  elif [ -n "$WARNS" ]; then
    bad "build warnings ($(printf '%s\n' "$WARNS" | wc -l | tr -d ' '))"
    printf '%s\n' "$WARNS" | head -n 20 | sed 's/^/         /'
    note "log: ${LOG#"$REPO_ROOT"/}"
    record build fail "warnings"; FAILED=1
  else
    ok "build clean"
    record build pass ""
  fi
fi

# ------------------------------------------------------------------- format --
if [ "$DO_FORMAT" = 1 ]; then
  head_line "format (per project, default severity, policy from .editorconfig)"
  FORMAT_HITS=0
  FORMAT_SKIPPED=0
  FORMAT_FINDINGS=()
  for PROJ in "${PROJECTS[@]}"; do
    [ -f "$PROJ" ] || { note "$PROJ missing, skipped"; continue; }
    PROJ_DIR="$(dirname "$PROJ")"
    NAME="$(basename "$PROJ" .csproj)"
    LOG="$LOG_DIR/format-$NAME.log"
    FORMAT_ARGS=()
    if [ "$SCOPED" = 1 ]; then
      # Only the paths this branch touched.  `--include` wants them relative to
      # the repository root (a project-relative path silently checks nothing),
      # so the git paths pass through as they are.
      mapfile -t INCLUDE < <(git status --porcelain | awk '{print $NF}' \
        | grep -E '\.(cs|xaml|axaml)$' \
        | while read -r f; do
            case "$f" in "$PROJ_DIR"/*) printf '%s\n' "$f" ;; esac
          done)
      if [ "${#INCLUDE[@]}" -eq 0 ]; then
        FORMAT_SKIPPED=$((FORMAT_SKIPPED + 1)); continue
      fi
      FORMAT_ARGS=(--include "${INCLUDE[@]}")
    fi
    dotnet format "$PROJ" --verify-no-changes "${FORMAT_ARGS[@]}" >"$LOG" 2>&1
    CODE=$?
    if [ "$CODE" -ne 0 ]; then
      bad "$PROJ (exit $CODE)"
      FORMAT_HITS=$((FORMAT_HITS + 1))
      # `dotnet format` also walks a project's references, so the same finding
      # arrives once per project that pulls it in — collect and print once.
      FOUND=0
      while IFS= read -r LINE; do
        [ -n "$LINE" ] && { FORMAT_FINDINGS+=("$LINE"); FOUND=1; }
      done < <(diagnostics "$LOG")
      if [ "$FOUND" = 0 ]; then
        FORMAT_FINDINGS+=("$PROJ: no diagnostic line (whitespace only) — ${LOG#"$REPO_ROOT"/}")
      fi
    else
      [ "$QUIET" = 0 ] && ok "$PROJ"
    fi
  done
  if [ "$FORMAT_HITS" -gt 0 ]; then
    printf '%s\n' "${FORMAT_FINDINGS[@]}" | sort -u | head -n 30 | sed 's/^/         /'
    note "logs: ${LOG_DIR#"$REPO_ROOT"/}/format-*.log"
    record format fail "$FORMAT_HITS project(s)"; FAILED=1
  else
    if [ "$SCOPED" = 1 ] && [ "$FORMAT_SKIPPED" -gt 0 ]; then
      ok "format clean" "$FORMAT_SKIPPED project(s) untouched"
    else
      ok "format clean"
    fi
    record format pass ""
  fi
fi

# --------------------------------------------------------------------- test --
if [ "$DO_TEST" = 1 ]; then
  head_line "test (MTP: no --nologo / -clp / -tl)"
  LOG="$LOG_DIR/test.log"
  dotnet test --solution "$SOLUTION" --results-directory artifacts/TestResults >"$LOG" 2>&1
  CODE=$?
  TOTAL="$(grep -oE '(总计|Total)[[:space:]]*:[[:space:]]*[0-9]+' "$LOG" | grep -oE '[0-9]+$' | tail -1)"
  FAILS="$(grep -oE '(失败|Failed)[[:space:]]*:[[:space:]]*[0-9]+' "$LOG" | grep -oE '[0-9]+$' | tail -1)"
  if [ "$CODE" -ne 0 ]; then
    bad "tests failed (exit $CODE)"
    grep -E "^[[:space:]]*(失败|Failed|错误)" "$LOG" | head -n 20 | sed 's/^/         /'
    note "log: ${LOG#"$REPO_ROOT"/}"
    record test fail "exit $CODE"; FAILED=1
  elif [ -z "${TOTAL:-}" ] || [ "$TOTAL" = 0 ]; then
    bad "the runner reported no tests — see AGENTS.md Common Traps #1"
    note "log: ${LOG#"$REPO_ROOT"/}"
    record test fail "no tests ran"; FAILED=1
  else
    ok "$TOTAL tests" "failed: ${FAILS:-0}"
    record test pass "$TOTAL tests"
  fi
fi

# ------------------------------------------------------------------ message --
if [ -n "$MESSAGE" ]; then
  head_line "message ($MESSAGE)"
  if [ "$MESSAGE" = "-" ]; then RAW="$(cat)"; else RAW="$(cat "$MESSAGE")"; fi
  MSG="$(printf '%s\n' "$RAW" | sed 's/^#.*$//' | sed -e :a -e '/^\n*$/{$d;N;};/\n$/ba')"
  SUBJECT="$(printf '%s\n' "$MSG" | head -n 1)"
  PROBLEMS=()
  if ! printf '%s' "$SUBJECT" | grep -qE "^($(IFS='|'; echo "${COMMIT_TYPES[*]}"))(\([^)]+\))?: .+"; then
    PROBLEMS+=("subject is not '<type>: <subject>' with a known type (${COMMIT_TYPES[*]})")
  fi
  [ "${#SUBJECT}" -gt "$SUBJECT_MAX" ] && PROBLEMS+=("subject is ${#SUBJECT} columns (limit $SUBJECT_MAX)")
  printf '%s' "$SUBJECT" | grep -q '\.$' && PROBLEMS+=("subject ends with a period")
  printf '%s' "$SUBJECT" | grep -qE '^[a-z]+(\([^)]+\))?: [A-Z]' \
    && PROBLEMS+=("subject after the colon starts uppercase — use the imperative lower case")
  BODY="$(printf '%s\n' "$MSG" | tail -n +2)"
  if [ -n "$(printf '%s' "$BODY" | tr -d '[:space:]')" ]; then
    if [ -n "$(printf '%s\n' "$MSG" | sed -n '2p')" ]; then
      PROBLEMS+=("line 2 must be blank (subject, blank, body)")
    fi
    while IFS= read -r LINE; do
      [ "${#LINE}" -gt "$BODY_MAX" ] \
        && PROBLEMS+=("body line is ${#LINE} columns (limit $BODY_MAX): ${LINE:0:40}...")
    done < <(printf '%s\n' "$BODY")
  fi
  if [ "${#PROBLEMS[@]}" -gt 0 ]; then
    bad "message"
    for P in "${PROBLEMS[@]}"; do note "$P"; done
    record message fail "${#PROBLEMS[@]} problem(s)"; FAILED=1
  else
    ok "message" "${#SUBJECT} column subject"
    record message pass ""
  fi
fi

# ------------------------------------------------------------------ summary --
head_line "summary"
for i in "${!STEP_NAMES[@]}"; do
  if [ "${STEP_VERDICTS[$i]}" = pass ]; then
    printf '%s  PASS%s %-8s %s\n' "$C_GREEN" "$C_OFF" "${STEP_NAMES[$i]}" "${STEP_NOTES[$i]}"
  else
    printf '%s  FAIL%s %-8s %s\n' "$C_RED" "$C_OFF" "${STEP_NAMES[$i]}" "${STEP_NOTES[$i]}"
  fi
done
if [ "$FAILED" = 0 ]; then
  printf '\n%s  the gate passed%s\n' "$C_GREEN" "$C_OFF"
  exit 0
fi
printf '\n%s  the gate failed — fix what it named and run it again%s\n' "$C_RED" "$C_OFF"
exit 1
