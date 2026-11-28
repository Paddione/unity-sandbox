#!/usr/bin/env bash
# shotcheck-windows.sh — visual self-check of the staged Windows player.
#
# Launches Games\cinderreach\cinderreach.exe with -shotcheck (DiabloShotCheck):
# the game drives the hero through the skill poses, saves screenshots into
# <game folder>\shotcheck and quits. The PNGs are copied to artifacts/shotcheck
# so agents can view them. A game window opens briefly on the Windows desktop.
#
# Usage (repo root, after stage-windows.sh): bash scripts/shotcheck-windows.sh [--autoplay]
#   --autoplay: launch with -autoplay -autoplay-out <game>/autoplay (presentation S5
#   perf proof) instead of -shotcheck; copies back autoplay/*.log for fps-gate.
# Env: SHOTCHECK_EXTRA_ARGS (extra game flags, e.g. -locomotion-shots), SHOTCHECK_GAME (default /mnt/c/Users/PatrickKorczewski/Games/cinderreach),
#      SHOTCHECK_TIMEOUT_S (default 120, autoplay default 600), SHOTCHECK_SIZE (default 1600x900,
#      S5 proof uses 1920x1080 via SHOTCHECK_SIZE=1920x1080).
# Saves: automation writes automation-save.json, never the player's cinderreach-save.json
#   (same productName = same persistentDataPath as the installed game).
# Exit: 0 shots taken, 1 missing game/shots, 2 timeout, 3 game already running.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

GAME="${SHOTCHECK_GAME:-/mnt/c/Users/PatrickKorczewski/Games/cinderreach}"
EXE="cinderreach.exe"
MODE="shotcheck"
for arg in "$@"; do
  if [ "$arg" = "--autoplay" ] || [ "$arg" = "-autoplay" ]; then MODE="autoplay"; fi
done
if [ "$MODE" = "autoplay" ]; then
  TIMEOUT="${SHOTCHECK_TIMEOUT_S:-600}"
else
  TIMEOUT="${SHOTCHECK_TIMEOUT_S:-120}"
fi
SIZE="${SHOTCHECK_SIZE:-1600x900}"
# Extra game flags (e.g. -locomotion-shots); spliced into cmd.exe, so only -word tokens.
EXTRA="${SHOTCHECK_EXTRA_ARGS:-}"
[[ -z "$EXTRA" || "$EXTRA" =~ ^(-[a-z][a-z-]*)( -[a-z][a-z-]*)*$ ]] || { echo "shotcheck: bad SHOTCHECK_EXTRA_ARGS '$EXTRA'" >&2; exit 1; }
OUT="$PWD/artifacts/shotcheck"

# SIZE is spliced into a cmd.exe command string: accept digits only.
[[ "$SIZE" =~ ^[0-9]{3,5}x[0-9]{3,5}$ ]] || { echo "shotcheck: bad SHOTCHECK_SIZE '$SIZE' (want WxH)" >&2; exit 1; }

[[ -f "$GAME/$EXE" ]] || { echo "shotcheck: no game at $GAME/$EXE (run stage-windows.sh)" >&2; exit 1; }
if tasklist.exe /FI "IMAGENAME eq $EXE" 2>/dev/null | grep -qi "$EXE"; then
  echo "shotcheck: $EXE is already running — close it first." >&2
  exit 3
fi

rm -rf "$GAME/shotcheck"
if [ "$MODE" = "autoplay" ]; then
  echo "shotcheck: launching $EXE -autoplay (${SIZE}, timeout ${TIMEOUT}s)"
  rc=0
  (cd "$GAME" && timeout "$TIMEOUT" cmd.exe /c "$EXE -autoplay $EXTRA -autoplay-out autoplay -save-file automation-save.json -screen-fullscreen 0 -screen-width ${SIZE%x*} -screen-height ${SIZE#*x} -logFile autoplay.log" >/dev/null 2>&1) || rc=$?
  if [ "$rc" -eq 124 ]; then
    taskkill.exe /IM "$EXE" /F >/dev/null 2>&1 || true
    echo "shotcheck: autoplay timed out after ${TIMEOUT}s" >&2
    grep "\[Autoplay\]\|\[Perf\]" "$GAME/autoplay.log" >&2 || true
    exit 2
  fi
  grep "\[Autoplay\]\|\[Perf\]\|DONE" "$GAME/autoplay.log" || true
  mkdir -p "$PWD/artifacts"
  cp "$GAME/autoplay.log" "$PWD/artifacts/autoplay-windows.log" || true
  echo "shotcheck: autoplay log -> artifacts/autoplay-windows.log (game exit $rc)"
  echo "shotcheck: run python3 scripts/fps-gate.py artifacts/autoplay-windows.log --min-fps 60 --resolution ${SIZE} --skip-first 1"
  exit "$rc"
fi
echo "shotcheck: launching $EXE -shotcheck (${SIZE}, timeout ${TIMEOUT}s)"
rc=0
# drvfs copies carry no exec bit: start through cmd.exe, which inherits the
# game folder as its working directory from this (cd).
(cd "$GAME" && timeout "$TIMEOUT" cmd.exe /c "$EXE -shotcheck $EXTRA -save-file automation-save.json -screen-fullscreen 0 -screen-width ${SIZE%x*} -screen-height ${SIZE#*x} -logFile shotcheck.log" >/dev/null 2>&1) || rc=$?
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
