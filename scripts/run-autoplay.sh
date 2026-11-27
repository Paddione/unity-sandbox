#!/usr/bin/env bash
# run-autoplay.sh — build the Linux player and run the Cinderreach session
# autoplay with result JSON + FPS gate (cinderreach-loop-guard S1).
#
# Steps:
#  1. compile pass (SandboxBuilder.CompileCheck), then build via
#     run-unity-build.sh (SandboxBuilder.BuildDiabloZoneBuild) into
#     <repo>/builds/autoplay-player/ of THIS checkout (skip: AUTOPLAY_SKIP_BUILD=1);
#  2. run the player at 1280x720 windowed with
#     -autoplay -autoplay-god -autoplay-out <out> -autoplay-commit <sha>
#     (no -batchmode: it would not render and the FPS gate would measure noise).
#     Display (AUTOPLAY_DISPLAY=auto|wslg|xvfb, default auto): wslg renders on
#     the GPU through the WSLg X server (/tmp/.X11-unix/X0) and Mesa d3d12
#     (/dev/dxg), measured 47+ fps worst window on 2026-11-18. xvfb renders on
#     llvmpipe (CPU, ~6 fps) and only proves the session, never the FPS gate.
#     auto picks wslg when both exist; the window shows on the desktop ~2 min.
#     xvfb-gpu renders off-screen in Xvfb through Mesa d3d12 on the GPU named by
#     AUTOPLAY_GPU_ADAPTER (substring match, default 3060): no desktop window,
#     measured median 45 fps / worst window 13 fps on the RTX 3060 Ti 2026-11-27;
#  3. hard timeout (AUTOPLAY_TIMEOUT, default 2700 s) -> exit 2 (hang);
#  4. fps-gate.py --min-fps 30 --resolution 1280x720 --skip-first 1 on the log,
#     advisory only since 2026-11-22: performance is proven on Windows with the
#     GPU (scripts/shotcheck-windows.sh --autoplay, 60 fps @1080p). Set
#     AUTOPLAY_FPS_STRICT=1 to let the Linux gate fail the run again.
#
# Exit = autoplay exit (max with fps-gate only under AUTOPLAY_FPS_STRICT=1);
# 2 = hang, 3 = Unity project busy (build).
# Usage (repo root): bash scripts/run-autoplay.sh [out_dir] [extra player args...]
#   out_dir default: artifacts/autoplay/<YYYY-MM-DD>/
#   extra player args are appended, e.g. -seed 1 (replayability).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
OUT="${1:-$ROOT/artifacts/autoplay/$(date +%F)}"
shift || true
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"
TIMEOUT="${AUTOPLAY_TIMEOUT:-2700}" # quest mode: Lantern Hold trips between zones, second Barrens pass
MIN_FPS="${AUTOPLAY_MIN_FPS:-30}"
PLAYER_DIR="$ROOT/builds/autoplay-player"
PLAYER="$PLAYER_DIR/cinderreach"
LOG="$OUT/autoplay-player.log"
COMMIT="$(git -C "$ROOT" rev-parse HEAD 2>/dev/null || echo unknown)"

if [ "${AUTOPLAY_SKIP_BUILD:-0}" != 1 ]; then
  # Separate compile session first: building in the session that recompiles
  # changed scripts produced a player with every scene script "missing".
  # unity-warmup.sh also rebinds runtime scripts whose MonoScript lost its class.
  bash "$ROOT/scripts/unity-warmup.sh" "$OUT"
  echo "run-autoplay: building Linux player -> $PLAYER"
  UNITY_BUILD_EXTRA_ARGS="-buildOut $PLAYER" bash "$ROOT/scripts/run-unity-build.sh" \
    SandboxBuilder.BuildDiabloZoneBuild "DiabloZone build result: Succeeded" 1500 \
    "$OUT/build.log"
fi
[ -x "$PLAYER" ] || { echo "run-autoplay: player missing: $PLAYER" >&2; exit 1; }
if [ "${AUTOPLAY_BUILD_ONLY:-0}" = 1 ]; then exit 0; fi
MODE="${AUTOPLAY_DISPLAY:-auto}"
if [ "$MODE" = auto ]; then
  if [ -S /tmp/.X11-unix/X0 ] && [ -e /dev/dxg ]; then MODE=wslg; else MODE=xvfb; fi
fi
case "$MODE" in
  wslg) LAUNCH=(env DISPLAY=:0 GALLIUM_DRIVER=d3d12) ;;
  xvfb)
    command -v xvfb-run >/dev/null || { echo "run-autoplay: xvfb-run missing" >&2; exit 1; }
    LAUNCH=(xvfb-run -a -s "-screen 0 1280x720x24") ;;
  xvfb-gpu)
    command -v xvfb-run >/dev/null || { echo "run-autoplay: xvfb-run missing" >&2; exit 1; }
    [ -e /dev/dxg ] || { echo "run-autoplay: xvfb-gpu needs /dev/dxg (WSL GPU)" >&2; exit 1; }
    LAUNCH=(env GALLIUM_DRIVER=d3d12 MESA_D3D12_DEFAULT_ADAPTER_NAME="${AUTOPLAY_GPU_ADAPTER:-3060}"
      xvfb-run -a -s "-screen 0 1280x720x24") ;;
  *) echo "run-autoplay: AUTOPLAY_DISPLAY must be auto|wslg|xvfb|xvfb-gpu, got $MODE" >&2; exit 1 ;;
esac

rm -f "$OUT/autoplay-result.json"
echo "run-autoplay: running autoplay (display $MODE, timeout ${TIMEOUT}s, log $LOG)"
set +e
timeout --kill-after=30 "$TIMEOUT" "${LAUNCH[@]}" \
  "$PLAYER" -screen-width 1280 -screen-height 720 -screen-fullscreen 0 \
  -autoplay -autoplay-god -autoplay-out "$OUT" -autoplay-commit "$COMMIT" \
  -logFile "$LOG" "$@"
RC=$?
set -e
if [ "$RC" -eq 124 ] || [ "$RC" -eq 137 ]; then
  echo "run-autoplay: HANG after ${TIMEOUT}s (exit 2)" >&2
  tail -n 20 "$LOG" >&2 || true
  exit 2
fi
grep -a '\[Autoplay\]' "$LOG" | tail -n 25 || true

if [ ! -f "$OUT/autoplay-result.json" ]; then
  echo "run-autoplay: no autoplay-result.json in $OUT (player exit $RC)" >&2
  if [ "$RC" -eq 0 ]; then
    echo "run-autoplay: the player shut down cleanly before the session finished;" \
      "its window was most likely closed on the desktop" >&2
  fi
  exit 1
fi

RENDERER="$(grep -a 'Renderer:' "$LOG" | head -n 1 || true)"
echo "$RENDERER"
if [ "$MODE" = xvfb-gpu ] && ! grep -q 'D3D12' <<<"$RENDERER"; then
  echo "run-autoplay: WARNING xvfb-gpu fell back to software rendering (no D3D12 renderer)" >&2
fi
set +e
python3 "$ROOT/scripts/fps-gate.py" "$LOG" --min-fps "$MIN_FPS" --resolution 1280x720 --skip-first 1
FPS_RC=$?
set -e

EXIT=$RC
if [ "${AUTOPLAY_FPS_STRICT:-0}" = "1" ]; then
  [ "$FPS_RC" -gt "$EXIT" ] && EXIT=$FPS_RC
elif [ "$FPS_RC" -ne 0 ]; then
  echo "run-autoplay: Linux fps-gate is advisory (Windows GPU shotcheck is the perf gate)"
fi
echo "run-autoplay: player exit $RC, fps-gate exit $FPS_RC -> $EXIT (result $OUT/autoplay-result.json)"
exit "$EXIT"
