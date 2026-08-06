#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
if [ "$#" -eq 0 ]; then
  dotnet run --project "$SCRIPT_DIR/_build/_build.csproj" -- --root "$SCRIPT_DIR"
elif [[ "$1" == -* ]]; then
  dotnet run --project "$SCRIPT_DIR/_build/_build.csproj" -- --root "$SCRIPT_DIR" "$@"
else
  dotnet run --project "$SCRIPT_DIR/_build/_build.csproj" -- --root "$SCRIPT_DIR" --target "$@"
fi
