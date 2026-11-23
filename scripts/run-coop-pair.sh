#!/usr/bin/env bash
# run-coop-pair.sh — two-process co-op proof (cinderreach-coop-session S1).
#
# Builds the Linux player like run-autoplay.sh (skip: AUTOPLAY_SKIP_BUILD=1),
# then starts a host and a client on this machine:
#   host:   -host -port $PORT -autoplay -autoplay-god -coop-autoplay host
#           (SessionAutoplay waits for the client, then plays the full route)
#   client: -join 127.0.0.1:$PORT -autoplay-god -coop-autoplay client
#           (CoopAutoplay follows the host through Lantern Hold and the Fen)
# COOP_SERVER=1 (cinderreach-dedicated-server): a headless -server without a hero instead of the host;
# client 1 leads (-coop-autoplay lead), the others follow; the server must quit by itself afterwards.
# Each writes autoplay-result.json (role host|client) into its own directory
# and uses its own save file (-save-file), so the two never share a save.
# Display as in run-autoplay.sh (COOP_DISPLAY=auto|wslg|xvfb). No FPS gate:
# two players share one GPU, the pair proves content, not frame rate.
# Classes: COOP_HOST_CLASS / COOP_CLIENT_CLASS set the -class flag per side
# (party-roles S5: e.g. host fenwalker + client rogue to prove role replacement).
# Group (coop-trio): COOP_CLIENTS=N (1..3, default 1) starts N clients; the host
# waits for all of them (-coop-peers N+1). Client k>1 writes to client<k>/,
# saves to coop-client<k>.json and takes its class from COOP_CLIENT<k>_CLASS.
# COOP_CLIENT_GOD=0 drops -autoplay-god on the clients (mortal clients: they
# take real damage, so healers have hurt allies to mend; the host stays god).
#
# Exit = max(host, client); 2 = hang (COOP_TIMEOUT, default 1200 s).
# Usage (repo root): bash scripts/run-coop-pair.sh [out_dir]
#
# Relay mode (S6): COOP_RELAY=1 hosts via `-host-relay` (Sessions SDK,
# join code from the host log) and joins the client via `-join-code CODE` —
# same route, same verdicts, UGS transport instead of direct UTP.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
OUT="${1:-$ROOT/artifacts/coop/$(date +%F)}"
CLIENTS="${COOP_CLIENTS:-1}"
case "$CLIENTS" in 1|2|3) ;; *) echo "run-coop-pair: COOP_CLIENTS must be 1..3, got $CLIENTS" >&2; exit 1 ;; esac
cdir() { [ "$1" -eq 1 ] && echo client || echo "client$1"; }   # client k -> out subdir
csave() { [ "$1" -eq 1 ] && echo coop-client.json || echo "coop-client$1.json"; }
cclass() { if [ "$1" -eq 1 ]; then echo "${COOP_CLIENT_CLASS:-}"; else local v="COOP_CLIENT$1_CLASS"; echo "${!v:-}"; fi; }
SERVER="${COOP_SERVER:-0}"
HOSTDIR=host; [ "$SERVER" = 1 ] && HOSTDIR=server
crole() { if [ "$SERVER" != 1 ]; then echo client; elif [ "$1" -eq 1 ]; then echo lead; else echo follow; fi; }
mkdir -p "$OUT/$HOSTDIR"
for k in $(seq 1 "$CLIENTS"); do mkdir -p "$OUT/$(cdir "$k")"; done
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

COMMON=(-screen-width 960 -screen-height 540 -screen-fullscreen 0 -autoplay-commit "$COMMIT")
GOD=(-autoplay-god)
CLIENT_GOD=(-autoplay-god)
[ "${COOP_CLIENT_GOD:-1}" = 0 ] && CLIENT_GOD=()
rm -f "$OUT"/*/autoplay-result.json
# Fresh saves per run (S6): loot accumulates in the bag across runs, and a
# full bag would fail the deterministic town potion buy. Previous saves are
# archived with a timestamp, never silently deleted.
SAVEDIR="${XDG_CONFIG_HOME:-$HOME/.config}/unity3d/Unity Technologies/Cinderreach"
for s in coop-host.json $(for k in $(seq 1 "$CLIENTS"); do csave "$k"; done); do
  if [ -f "$SAVEDIR/$s" ]; then
    bak="$SAVEDIR/$s.bak-$(date +%F-%H%M%S)"
    mv "$SAVEDIR/$s" "$bak" && echo "run-coop-pair: archived stale save to $bak"
  fi
done

PEERS=(-coop-peers $((CLIENTS + 1)))
[ "$SERVER" = 1 ] && PEERS=(-coop-peers "$CLIENTS")   # dedicated server: only clients are players
start_client() { # k target-args...
  local k=$1; shift
  local cls; cls="$(cclass "$k")"
  echo "run-coop-pair: client $k -> $* ${cls:+(class $cls)}"
  timeout --kill-after=30 "$TIMEOUT" "${LAUNCH[@]}" "$PLAYER" "${COMMON[@]}" ${CLIENT_GOD[@]+"${CLIENT_GOD[@]}"} \
    "$@" -coop-autoplay "$(crole "$k")" ${SERVER_PEERS[@]+"${SERVER_PEERS[@]}"} ${cls:+-class "$cls"} -save-file "$(csave "$k")" \
    -autoplay-out "$OUT/$(cdir "$k")" -logFile "$OUT/$(cdir "$k")/player.log" &
  CLIENT_PIDS+=($!)
}
CLIENT_PIDS=()
SERVER_PEERS=()
set +e
if [ "$SERVER" = 1 ]; then
  # cinderreach-dedicated-server S1: headless server without a hero, clients lead/follow.
  SERVER_PEERS=("${PEERS[@]}")
  # S2: COOP_RELAY=1 publishes the server via Relay + Lobby under a run-unique name.
  SERVER_NET=(-port "$PORT"); JOIN=(-join "127.0.0.1:$PORT")
  if [ "${COOP_RELAY:-0}" = 1 ]; then
    NAME="Cinderreach Server test-$$"
    SERVER_NET=(-server-relay -server-name "$NAME"); JOIN=(-join-server -server-name "$NAME")
  fi
  echo "run-coop-pair: dedicated server ${SERVER_NET[*]} (headless, timeout ${TIMEOUT}s, clients $CLIENTS)"
  timeout --kill-after=30 "$TIMEOUT" "$PLAYER" -batchmode -nographics -server "${SERVER_NET[@]}" \
    -logFile "$OUT/server/player.log" &
  HOST_PID=$!
  for _ in $(seq 1 60); do
    sleep 2
    if [ "${COOP_RELAY:-0}" = 1 ]; then grep -aq '\[Server\] published' "$OUT/server/player.log" 2>/dev/null && break
    else grep -aq '\[Server\] ready' "$OUT/server/player.log" 2>/dev/null && break; fi
    kill -0 "$HOST_PID" 2>/dev/null || break
  done
  for k in $(seq 1 "$CLIENTS"); do start_client "$k" "${JOIN[@]}"; sleep 3; done
elif [ "${COOP_RELAY:-0}" = 1 ]; then
  echo "run-coop-pair: host via relay (display $MODE, timeout ${TIMEOUT}s, clients $CLIENTS)"
  timeout --kill-after=30 "$TIMEOUT" "${LAUNCH[@]}" "$PLAYER" "${COMMON[@]}" \
    "${GOD[@]}" -host-relay -autoplay -coop-autoplay host "${PEERS[@]}" ${COOP_HOST_CLASS:+-class "$COOP_HOST_CLASS"} -save-file coop-host.json \
    -autoplay-out "$OUT/host" -logFile "$OUT/host/player.log" &
  HOST_PID=$!
  echo "run-coop-pair: waiting for relay join code..."
  CODE=""
  for _ in $(seq 1 120); do
    sleep 5
    CODE=$(grep -a -o 'RELAY_JOIN_CODE=[A-Z0-9]*' "$OUT/host/player.log" 2>/dev/null | tail -n 1 | cut -d= -f2)
    [ -n "$CODE" ] && break
    kill -0 "$HOST_PID" 2>/dev/null || break
  done
  if [ -z "$CODE" ]; then echo "run-coop-pair: no relay join code from host" >&2; fi
  for k in $(seq 1 "$CLIENTS"); do start_client "$k" -join-code "$CODE"; sleep 3; done
else
  echo "run-coop-pair: host on port $PORT (display $MODE, timeout ${TIMEOUT}s, clients $CLIENTS)"
  timeout --kill-after=30 "$TIMEOUT" "${LAUNCH[@]}" "$PLAYER" "${COMMON[@]}" \
    "${GOD[@]}" -host -port "$PORT" -autoplay -coop-autoplay host "${PEERS[@]}" ${COOP_HOST_CLASS:+-class "$COOP_HOST_CLASS"} -save-file coop-host.json \
    -autoplay-out "$OUT/host" -logFile "$OUT/host/player.log" &
  HOST_PID=$!
  sleep 8
  for k in $(seq 1 "$CLIENTS"); do start_client "$k" -join "127.0.0.1:$PORT"; sleep 3; done
fi
CLIENT_RCS=()
for pid in "${CLIENT_PIDS[@]}"; do wait "$pid"; CLIENT_RCS+=($?); done
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
EXIT=0
for k in $(seq 1 "$CLIENTS"); do
  d="$(cdir "$k")"
  verdict "$d" "${CLIENT_RCS[$((k - 1))]}"; C=$?
  grep -a '\[CoopAutoplay\]' "$OUT/$d/player.log" | tail -n 8 || true
  [ "$C" -gt "$EXIT" ] && EXIT=$C
done
if [ "$SERVER" = 1 ]; then
  # The server has no autoplay result: it must quit on its own (exit 0) once everyone left.
  if [ "$HOST_RC" -eq 0 ] && grep -aq '\[Server\] session over' "$OUT/server/player.log"; then
    echo "run-coop-pair: server quit after the session (exit 0)"; H=0
  else
    echo "run-coop-pair: server did not end cleanly (exit $HOST_RC)" >&2; H=1
  fi
  grep -a '\[Server\]\|\[Zone\]' "$OUT/server/player.log" | tail -n 12 || true
else
  verdict host "$HOST_RC"; H=$?
fi
set -e
[ "$H" -gt "$EXIT" ] && EXIT=$H
echo "run-coop-pair: host $H, clients ${CLIENTS} -> $EXIT (logs $OUT/*/player.log)"
exit "$EXIT"
