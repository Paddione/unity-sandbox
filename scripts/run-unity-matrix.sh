#!/usr/bin/env bash
# run-unity-matrix.sh — one Linux + Windows + Server matrix build (track P4).
#
# Reuses run-unity-build.sh per target (lock-gate exit 3, warm-editor
# stop/restart trap, heartbeat, timeout exit 2, marker exit 1) and writes one
# BUILD_INFO.json per target next to its artifact. Server subtarget is the
# default for remote logic tests (player builds segfault on hosts without
# GPU: RC=139 at graphics init); player builds run locally with GPU only.
# The run-unity-build.sh reset-failed trap fix also applies here transitively.
#
# Usage (from the repo root):
#   bash scripts/run-unity-matrix.sh [timeout_s] [logdir]
# Example:
#   bash scripts/run-unity-matrix.sh 1500 /tmp/matrix-logs
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
TIMEOUT=${1:-1500}
LOGDIR=${2:-/tmp/unity-matrix-$(date +%Y%m%d-%H%M%S)}
mkdir -p "$LOGDIR"
SHA=$(git rev-parse --short HEAD 2>/dev/null || echo "uncommitted")

# method | marker | artifact-dir | label
TARGETS=(
  "SandboxBuilder.BuildDiabloZoneBuild|DiabloZone build result: Succeeded|builds/diablo-zone|linux-player"
  "SandboxBuilder.BuildDiabloZoneWindows|DiabloZone Windows build result: Succeeded|builds/diablo-zone-windows|windows-player"
  "SandboxBuilder.BuildDiabloZoneServer|DiabloZone server build result: Succeeded|builds/diablo-server|linux-server"
)

failed=0
for spec in "${TARGETS[@]}"; do
  IFS='|' read -r method marker artdir label <<< "$spec"
  log="$LOGDIR/$label.log"
  echo "=== matrix: $label ($method) ==="
  if bash scripts/run-unity-build.sh "$method" "$marker" "$TIMEOUT" "$log"; then
    size=$(du -sm "$artdir" 2>/dev/null | cut -f1)
    stamp=$(date -u +%FT%TZ)
    printf '{"target":"%s","method":"%s","marker":"%s","commit":"%s","built_at":"%s","artifact_dir":"%s","artifact_mb":%s}\n' \
      "$label" "$method" "$marker" "$SHA" "$stamp" "$artdir" "${size:-0}" > "$artdir/BUILD_INFO.json"
    echo "matrix: $label ok, BUILD_INFO.json written."
  else
    echo "matrix: $label FAILED (see $log)." >&2
    failed=1
  fi
done
exit "$failed"
