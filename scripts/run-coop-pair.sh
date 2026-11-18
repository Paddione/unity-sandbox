#!/usr/bin/env bash
# run-coop-pair.sh — two-process co-op proof (cinderreach-coop-session S1).
#
# Builds the Linux player like run-autoplay.sh (skip: AUTOPLAY_SKIP_BUILD=1),
# then starts a host and a client on this machine:
#   host:   -host -port $PORT -autoplay -autoplay-god -coop-autoplay host
#           (SessionAutoplay waits for the client, then plays the full route)
#   client: -join 127.0.0.1:$PORT -autoplay-god -coop-autoplay client
#           (CoopAutoplay follows the host through Lantern Hold and the Fen)
# Each writes autoplay-result.json (role host|client) into its own directory
# and uses its own save file (-save-file), so the two never share a save.
# Display as in run-autoplay.sh (COOP_DISPLAY=auto|wslg|xvfb). No FPS gate:
# two players share one GPU, the pair proves content, not frame rate.
#
# Exit = max(host, client); 2 = hang (COOP_TIMEOUT, default 1200 s).
# Usage (repo root): bash scripts/run-coop-pair.sh [out_dir]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
OUT="${1:-$ROOT/artifacts/coop/$(date +%F)}"
mkdir -p "$OUT/host" "$OUT/client"
OUT="$(cd "$OUT" && pwd)"
TIMEOUT="${COOP_TIMEOUT:-1200}"
PORT="${COOP_PORT:-7790}"
PLAYER="$ROOT/builds/autoplay-player/cinderreach"
COMMIT="$(git -C "$ROOT" rev-parse HEAD 2>/dev/null || echo unknown)"

if [ "${AUTOPLAY_SKIP_BUILD:-0}" != 1 ]; then
  bash "$ROOT/scripts/unity-warmup.sh" "$OUT"
  echo "run-coop-pair: building Linux player -> $PLAYER"
  UNITY_BUILD_EXTRA_ARGS="-buildOut $PLAYER" bash "$ROOT/scripts/run-unity-build.sh" \
    SandboxBuilder.BuildDiabloZoneBuild "DiabloZone build result: Succeeded" 1500 "$OUT/build.log"
fi
[ -x "$PLAYER" ] || { echo "run-coop-pair: player missing: $PLAYER" >&2; exit 1; }

MODE="${COOP_DISPLAY:-auto}"
if [ "$MODE" = auto ]; then
  if [ -S /tmp/.X11-unix/X0 ] && [ -e /dev/dxg ]; then MODE=wslg; else MODE=xvfb; fi
fi
case "$MODE" in
  wslg) LAUNCH=(env DISPLAY=:0 GALLIUM_DRIVER=d3d12) ;;
  xvfb)
    command -v xvfb-run >/dev/null || { echo "run-coop-pair: xvfb-run missing" >&2; exit 1; }
    LAUNCH=(xvfb-run -a -s "-screen 0 960x540x24") ;;
  *) echo "run-coop-pair: COOP_DISPLAY must be auto|wslg|xvfb, got $MODE" >&2; exit 1 ;;
esac

COMMON=(-screen-width 960 -screen-height 540 -screen-fullscreen 0 -autoplay-god -autoplay-commit "$COMMIT")
rm -f "$OUT/host/autoplay-result.json" "$OUT/client/autoplay-result.json"

echo "run-coop-pair: host on port $PORT (display $MODE, timeout ${TIMEOUT}s)"
set +e
timeout --kill-after=30 "$TIMEOUT" "${LAUNCH[@]}" "$PLAYER" "${COMMON[@]}" \
  -host -port "$PORT" -autoplay -coop-autoplay host -save-file coop-host.json \
  -autoplay-out "$OUT/host" -logFile "$OUT/host/player.log" &
HOST_PID=$!
sleep 8
echo "run-coop-pair: client -> 127.0.0.1:$PORT"
timeout --kill-after=30 "$TIMEOUT" "${LAUNCH[@]}" "$PLAYER" "${COMMON[@]}" \
  -join "127.0.0.1:$PORT" -coop-autoplay client -save-file coop-client.json \
  -autoplay-out "$OUT/client" -logFile "$OUT/client/player.log" &
CLIENT_PID=$!
wait "$CLIENT_PID"; CLIENT_RC=$?
wait "$HOST_PID"; HOST_RC=$?
set -e

verdict() { # role rc
  local role=$1 rc=$2 json="$OUT/$1/autoplay-result.json"
  if [ "$rc" -eq 124 ] || [ "$rc" -eq 137 ]; then echo "run-coop-pair: $role HANG" >&2; return 2; fi
  if [ ! -f "$json" ]; then echo "run-coop-pair: $role wrote no result (exit $rc)" >&2; return 1; fi
  python3 - "$json" "$role" <<'PY'
import json, sys
d = json.load(open(sys.argv[1]))
print(f"run-coop-pair: {sys.argv[2]} exit={d['exit']} phases={','.join(d['phases'])} "
      f"exceptions={d['exceptions']} duration={d['duration_s']:.0f}s role={d.get('role')}")
sys.exit(d["exit"])
PY
}
set +e
verdict client "$CLIENT_RC"; C=$?
verdict host "$HOST_RC"; H=$?
set -e
grep -a '\[CoopAutoplay\]' "$OUT/client/player.log" | tail -n 8 || true
EXIT=$C
[ "$H" -gt "$EXIT" ] && EXIT=$H
echo "run-coop-pair: host $H, client $C -> $EXIT (logs $OUT/{host,client}/player.log)"
exit "$EXIT"
