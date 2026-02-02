#!/usr/bin/env bash
set -euo pipefail

# publish.sh - publish Release single-file builds for multiple runtimes
# Usage: ./publish.sh [--self-contained] [--include-mac] [--clean]

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="../src/MermaidCli/MermaidCli.csproj"
OUTDIR="../publish"

SELF_CONTAINED=false
INCLUDE_MAC=false
CLEAN=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --self-contained) SELF_CONTAINED=true; shift ;;
    --include-mac) INCLUDE_MAC=true; shift ;;
    --clean) CLEAN=true; shift ;;
    -h|--help)
      cat <<'USAGE'
Usage: publish.sh [--self-contained] [--include-mac] [--clean]

Options:
  --self-contained   Produce self-contained builds (default is framework-dependent)
  --include-mac      Attempt to publish macOS targets even when not running on macOS
  --clean            Remove existing publish/* output before publishing
  -h, --help         Show this help
USAGE
      exit 0
      ;;
    *) echo "Unknown arg: $1" >&2; exit 2 ;;
  esac
done

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found in PATH" >&2
  exit 1
fi

if [[ "$CLEAN" == true ]]; then
  echo "Cleaning $OUTDIR"
  rm -rf "$OUTDIR"
fi

mkdir -p "$OUTDIR"

# RIDs to publish
RIDS=(
  linux-x64
  linux-arm64
  win-x64
  win-arm64
  osx-x64
  osx-arm64
)

HOST_OS=$(uname -s)

for rid in "${RIDS[@]}"; do
  if [[ "$rid" == osx-* && "$HOST_OS" != Darwin && "$INCLUDE_MAC" != true ]]; then
    echo "Skipping $rid (not running on macOS). Use --include-mac to force."
    continue
  fi

  echo "Publishing for $rid..."

  OUT_PATH="$OUTDIR/$rid"
  mkdir -p "$OUT_PATH"

  SC_FLAG="/p:SelfContained=false"
  if [[ "$SELF_CONTAINED" == true ]]; then
    SC_FLAG="/p:SelfContained=true"
  fi

  dotnet publish "$PROJECT" -c Release -r "$rid" /p:PublishSingleFile=true $SC_FLAG -o "$OUT_PATH"

  echo "Published to $OUT_PATH"
done

echo "All done. Outputs are in $OUTDIR"
