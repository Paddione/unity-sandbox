#!/usr/bin/env bash
# run-unity-build.sh — hardened Unity batchmode build wrapper.
#
# Why this exists (2026-11-15, Ashen Fen builds): raw
# `Unity -batchmode ... -executeMethod` runs failed three ways in one day:
#  1. a second editor (the unity-sandbox-editor.service warm instance) holds
#     the project and the build aborts with a core dump;
#  2. Unity hangs in shutdown AFTER a successful build, burning hours of CPU
#     silently (3 h single-thread spin observed, outputs already written);
#  3. no heartbeat, so a stuck build looks identical to a slow one.
#
# What it does:
#  - refuses to start when another Unity holds this project (exit 3);
#    only a stale lockfile (no live holder) is removed;
#  - stops unity-sandbox-editor.service for the build and restarts it after
#    (trap-guarded), so the warm editor can neither block nor race the build
#    over Library/;
#  - pins job worker threads (-job-worker-count, default 10 of 12 cores);
#  - prints a heartbeat line every 60 s (elapsed + last progress line);
#  - hard timeout (default 1500 s): kills stragglers, clears the lock,
#    prints the log tail, exits 2 (distinguishes hang from build failure);
#  - checks the success marker in the log (exit 1 when missing).
#
# Usage (from the repo root):
#   bash scripts/run-unity-build.sh <ExecuteMethod> <success-marker> [timeout_s] [logfile]
#   (env UNITY_BUILD_EXTRA_ARGS appends editor args, see step 3)
# Example:
#   bash scripts/run-unity-build.sh SandboxBuilder.BuildDiabloZoneServer \
#     "DiabloZone server build result: Succeeded" 1200 /tmp/bm-server.log

set -euo pipefail

METHOD=${1:?usage: run-unity-build.sh <ExecuteMethod> <success-marker> [timeout_s] [logfile]}
MARKER=${2:?usage: run-unity-build.sh <ExecuteMethod> <success-marker> [timeout_s] [logfile]}
TIMEOUT=${3:-1500}
LOG=${4:-/tmp/unity-build-$(date +%Y%m%d-%H%M%S).log}

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/sandbox"
[ -d "$PROJECT/Assets" ] || { echo "run-unity-build: must run from the repo root (no $PROJECT/Assets)" >&2; exit 1; }

VERSION=$(awk '/^m_EditorVersion:/{print $2; exit}' "$PROJECT/ProjectSettings/ProjectVersion.txt")
EDITOR="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity}"
[ -x "$EDITOR" ] || { echo "run-unity-build: Unity editor missing: $EDITOR" >&2; exit 1; }

JOBS=${UNITY_BUILD_JOBS:-10}
SERVICE=unity-sandbox-editor.service
LOCKFILE="$PROJECT/Temp/UnityLockfile"

live_holders() {
  local holders
  holders=$(pgrep -f "Unity.*-projectPath $PROJECT" || true)
  holders=$(echo "$holders" | grep -v -e "^$$\$" -e "^$PPID\$" || true)
  echo "$holders"
}

# --- 1. Warm-editor service -------------------------------------------------
# Stop first: the keep-warm editor otherwise trips the lock gate on every run
# and races the build over Library/ (3 h single-thread spin observed).
SERVICE_STOPPED=0
if systemctl --user is-active --quiet "$SERVICE" 2>/dev/null; then
  echo "run-unity-build: stopping $SERVICE for the build."
  systemctl --user stop "$SERVICE"
  SERVICE_STOPPED=1
  for _ in $(seq 1 30); do
    [ -z "$(live_holders)" ] && break
    sleep 1
  done
fi
restore_service() {
  if [ "$SERVICE_STOPPED" = 1 ]; then
    echo "run-unity-build: restarting $SERVICE."
    systemctl --user start "$SERVICE" || true
  fi
  rm -f "$LOCKFILE"
}
trap restore_service EXIT

# --- 2. Project lock gate -------------------------------------------------
if [ -e "$LOCKFILE" ]; then
  if [ -n "$(live_holders)" ]; then
    echo "run-unity-build: project locked by live Unity: $(live_holders)" >&2
    echo "run-unity-build: refusing to build over a live editor (exit 3)" >&2
    exit 3
  fi
  echo "run-unity-build: removing stale lockfile (no live holder)."
  rm -f "$LOCKFILE"
fi

# --- 3. Build with heartbeat + hard timeout --------------------------------
echo "run-unity-build: $METHOD (timeout ${TIMEOUT}s, jobs $JOBS, log $LOG)"
# UNITY_BUILD_EXTRA_ARGS: optional extra editor args, word-split on purpose
# (e.g. "-buildOut /path/to/player" for SandboxBuilder.BuildDiabloZoneBuild).
# shellcheck disable=SC2086
"$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" \
  -job-worker-count "$JOBS" -executeMethod "$METHOD" -logFile "$LOG" ${UNITY_BUILD_EXTRA_ARGS:-} &
UNITY_PID=$!

START=$SECONDS
while kill -0 "$UNITY_PID" 2>/dev/null; do
  sleep 60
  ELAPSED=$((SECONDS - START))
  PHASE=$(grep -a -o "DisplayProgressbar: .*" "$LOG" 2>/dev/null | tail -1 || true)
  echo "run-unity-build: heartbeat ${ELAPSED}s alive ${PHASE:-starting up}"
  if [ "$ELAPSED" -ge "$TIMEOUT" ]; then
    echo "run-unity-build: TIMEOUT after ${ELAPSED}s — killing build tree." >&2
    kill -9 "$UNITY_PID" 2>/dev/null || true
    sleep 2
    STRAGGLERS=$(live_holders || true)
    [ -n "$STRAGGLERS" ] && kill -9 $STRAGGLERS 2>/dev/null || true
    echo "--- log tail ---" >&2
    tail -20 "$LOG" | cut -c1-160 >&2 || true
    exit 2
  fi
done
RC=0
wait "$UNITY_PID" || RC=$?

# --- 4. Result ---------------------------------------------------------------
trap - EXIT
restore_service
if [ "$RC" -ne 0 ]; then
  echo "run-unity-build: Unity exited $RC." >&2
  tail -20 "$LOG" | cut -c1-160 >&2 || true
  exit 1
fi
if grep -a -F -q "$MARKER" "$LOG"; then
  echo "run-unity-build: OK ($METHOD)."
  exit 0
fi
echo "run-unity-build: Unity exited 0 but marker missing: $MARKER" >&2
tail -20 "$LOG" | cut -c1-160 >&2 || true
exit 1
