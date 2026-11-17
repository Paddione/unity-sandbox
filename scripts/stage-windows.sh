#!/usr/bin/env bash
# stage-windows.sh — SSOT for putting the Windows player on the Windows host.
#
# One game, one folder: C:\Users\PatrickKorczewski\Games\cinderreach.
# Legacy folder Games\diablo-zone stays untouched (listed, never deleted).
# Earlier milestones staged by hand into extra folders (strike-named and
# fen-named) with the same exe, which left stale copies side by side.
# Every stage goes through this script; it mirrors (rsync --delete) the
# wrapper output builds/cinderreach-windows and refreshes BUILD_INFO.json.
#
# Usage (from the repo root, after the Windows build):
#   bash scripts/run-unity-build.sh SandboxBuilder.BuildDiabloZoneWindows \
#     "DiabloZone Windows build result: Succeeded" 900
#   bash scripts/stage-windows.sh
#
# Env: STAGE_SRC (default: the absolute output path of
#      SandboxBuilder.BuildDiabloZoneWindows, also when run from a worktree),
#      STAGE_DEST (default /mnt/c/Users/PatrickKorczewski/Games/cinderreach).
# Exit: 0 staged, 1 missing build, 3 game running (exe locked).

set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

SRC="${STAGE_SRC:-/home/patrick/unity-sandbox/builds/cinderreach-windows}"
DEST="${STAGE_DEST:-/mnt/c/Users/PatrickKorczewski/Games/cinderreach}"
EXE="cinderreach.exe"

if [[ ! -f "$SRC/$EXE" ]]; then
  echo "stage-windows: no build at $SRC/$EXE — run the Windows build first." >&2
  exit 1
fi

if command -v tasklist.exe >/dev/null 2>&1 \
   && tasklist.exe /FI "IMAGENAME eq $EXE" 2>/dev/null | grep -qi "$EXE"; then
  echo "stage-windows: $EXE is running on Windows — close the game first." >&2
  exit 3
fi

# Commit of the checkout that owns the build dir (the builder writes there).
commit="$(git -C "$SRC/../.." rev-parse --short HEAD 2>/dev/null || echo unknown)"
mb="$(du -sm "$SRC" | cut -f1)"
printf '{"target":"windows-player","method":"SandboxBuilder.BuildDiabloZoneWindows","commit":"%s","staged_at":"%s","artifact_dir":"%s","artifact_mb":%s}\n' \
  "$commit" "$(date -u +%FT%TZ)" "$SRC" "$mb" > "$SRC/BUILD_INFO.json"

mkdir -p "$DEST"
rsync -a --delete "$SRC/" "$DEST/"
cmp -s "$SRC/${EXE%.exe}_Data/level0" "$DEST/${EXE%.exe}_Data/level0"
echo "stage-windows: OK $SRC -> $DEST (commit $commit, ${mb} MB)."
