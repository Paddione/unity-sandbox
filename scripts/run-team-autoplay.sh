#!/usr/bin/env bash
# run-team-autoplay.sh — CI autoplay as a team (cinderreach-team-autoplay K2–K4).
#
# Two sessions on one dedicated server setup (run-coop-pair.sh, COOP_SERVER=1,
# three clients over UTP on 127.0.0.1):
#   normal:  fresh saves; client 1 fenwalker leads with -autoplay-god, clients 2/3
#            (lanternbearer, rogue) are mortal and follow. The team plays every
#            quest and all act bosses up to Thewes; that kill unlocks Kindled and
#            every client starts its next run on Kindled (campaign restarts, hero kept).
#   kindled: the same three save files, no -difficulty flag: the clients load
#            Kindled from their saves, the server adopts it from the first join.
# Each client runs with its own HOME/XDG profile under <out>/profiles/client<k>
# (kept across both sessions). Results: <out>/<session>/{server,client1..3}/ plus
# <session>/team-verdict.json (scripts/team_autoplay_verdict.py).
#
# Env: AUTOPLAY_SKIP_BUILD=1 reuses builds/autoplay-player, COOP_DISPLAY (default
# xvfb-gpu), TEAM_TIMEOUT per session (default 4200 s), TEAM_SESSIONS (default
# "normal kindled"), COOP_PORT base (default 28790; kindled uses +1).
# Exit = max over the sessions (verdict 1, hang 2).
# Usage (repo root): bash scripts/run-team-autoplay.sh [out_dir]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
OUT="${1:-$ROOT/artifacts/autoplay/$(date +%F)}"
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"
PROFILES="$OUT/profiles"
mkdir -p "$PROFILES"
SESSIONS="${TEAM_SESSIONS:-normal kindled}"
PORT="${COOP_PORT:-28790}"
BUILT="${AUTOPLAY_SKIP_BUILD:-0}"
EXIT=0

for session in $SESSIONS; do
  case "$session" in
    normal) keep=0; want=Normal ;;
    kindled) keep=1; want=Kindled ;;
    *) echo "run-team-autoplay: unknown session $session" >&2; exit 1 ;;
  esac
  echo "run-team-autoplay: session $session (expect $want, port $PORT)"
  set +e
  COOP_SERVER=1 COOP_CLIENTS=3 COOP_CLIENT_GOD=0 \
    COOP_CLIENT_CLASS=fenwalker COOP_CLIENT2_CLASS=lanternbearer COOP_CLIENT3_CLASS=rogue \
    COOP_CLIENT_HOMES="$PROFILES" COOP_KEEP_SAVES="$keep" COOP_PORT="$PORT" \
    COOP_TIMEOUT="${TEAM_TIMEOUT:-4200}" COOP_DISPLAY="${COOP_DISPLAY:-xvfb-gpu}" \
    AUTOPLAY_SKIP_BUILD="$BUILT" \
    bash "$ROOT/scripts/run-coop-pair.sh" "$OUT/$session"
  RC=$?
  set -e
  BUILT=1
  PORT=$((PORT + 1))
  # run-coop-pair names client 1's folder "client"; the team layout numbers all three.
  if [ -d "$OUT/$session/client" ]; then rm -rf "$OUT/$session/client1"; mv "$OUT/$session/client" "$OUT/$session/client1"; fi
  set +e
  python3 "$ROOT/scripts/team_autoplay_verdict.py" "$OUT/$session" --difficulty "$want"
  V=$?
  set -e
  [ "$RC" -eq 2 ] && V=2
  [ "$V" -gt "$EXIT" ] && EXIT=$V
  if [ "$session" = normal ] && [ "$V" -ne 0 ]; then
    echo "run-team-autoplay: normal session failed, Kindled session skipped" >&2
    break
  fi
done
echo "run-team-autoplay: exit $EXIT (results $OUT)"
exit "$EXIT"
