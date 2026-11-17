#!/usr/bin/env bash
# shotcheck-windows.sh — visual self-check of the staged Windows player.
#
# Launches Games\diablo-zone\diablo-zone.exe with -shotcheck (DiabloShotCheck):
# the game drives the hero through the skill poses, saves screenshots into
# <game folder>\shotcheck and quits. The PNGs are copied to artifacts/shotcheck
# so agents can view them. A game window opens briefly on the Windows desktop.
#
# Usage (repo root, after stage-windows.sh): bash scripts/shotcheck-windows.sh
# Env: SHOTCHECK_GAME (default /mnt/c/Users/PatrickKorczewski/Games/diablo-zone),
#      SHOTCHECK_TIMEOUT_S (default 120), SHOTCHECK_SIZE (default 1600x900).
# Exit: 0 shots taken, 1 missing game/shots, 2 timeout, 3 game already running.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

GAME="${SHOTCHECK_GAME:-/mnt/c/Users/PatrickKorczewski/Games/diablo-zone}"
EXE="diablo-zone.exe"
TIMEOUT="${SHOTCHECK_TIMEOUT_S:-120}"
SIZE="${SHOTCHECK_SIZE:-1600x900}"
OUT="$PWD/artifacts/shotcheck"

[[ -f "$GAME/$EXE" ]] || { echo "shotcheck: no game at $GAME/$EXE (run stage-windows.sh)" >&2; exit 1; }
if tasklist.exe /FI "IMAGENAME eq $EXE" 2>/dev/null | grep -qi "$EXE"; then
  echo "shotcheck: $EXE is already running — close it first." >&2
  exit 3
fi

rm -rf "$GAME/shotcheck"
echo "shotcheck: launching $EXE -shotcheck (${SIZE}, timeout ${TIMEOUT}s)"
rc=0
# drvfs copies carry no exec bit: start through cmd.exe, which inherits the
# game folder as its working directory from this (cd).
(cd "$GAME" && timeout "$TIMEOUT" cmd.exe /c "$EXE -shotcheck -screen-fullscreen 0 -screen-width ${SIZE%x*} -screen-height ${SIZE#*x} -logFile shotcheck.log" >/dev/null 2>&1) || rc=$?
if [ "$rc" -eq 124 ]; then
  taskkill.exe /IM "$EXE" /F >/dev/null 2>&1 || true
  echo "shotcheck: timed out after ${TIMEOUT}s" >&2
  grep "\[ShotCheck\]" "$GAME/shotcheck.log" >&2 || true
  exit 2
fi

grep "\[ShotCheck\]" "$GAME/shotcheck.log" || true
shopt -s nullglob
shots=("$GAME"/shotcheck/*.png)
if [ "${#shots[@]}" -eq 0 ]; then
  echo "shotcheck: no screenshots produced (game exit $rc)" >&2
  exit 1
fi
rm -rf "$OUT" && mkdir -p "$OUT"
cp "${shots[@]}" "$OUT/"
echo "shotcheck: ${#shots[@]} screenshots -> $OUT (game exit $rc)"
ls -1 "$OUT"
