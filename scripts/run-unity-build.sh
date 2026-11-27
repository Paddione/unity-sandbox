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
#    over Library/; only a build of the main checkout manages that shared
#    service — worktree builds leave it (and every MCP session on it) alone;
#  - serializes all builds/tests machine-wide via flock on
#    /tmp/unity-sandbox-editor.lock, so concurrent sessions queue instead
#    of interleaving stop/start and leaving the shared editor dead;
#  - pins job worker threads (-job-worker-count, default 10 of 12 cores);
#  - prints a heartbeat line every 60 s (elapsed, log silence, last line);
#  - stall watchdog: a log silent for UNITY_STALL_S (default 600 s) ends the
#    run like a timeout, and waiting runs end a stalled lock holder
#    (scripts/lib/unity-editor-lock.sh), so one frozen editor cannot block
#    the queue. The verdict is CPU-aware: silent AND idle ends the run,
#    silent but CPU-busy grants a fresh window (slow, not stuck);
#  - hard timeout (default 1500 s): kills stragglers, clears the lock,
#    prints the log tail, exits 2 (distinguishes hang from build failure);
#    a stall or timeout after the success marker counts as OK (shutdown hang);
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

# --- 0. Machine-wide build lock ----------------------------------------------
# All editor-owning runs (builds + tests, any checkout) queue on one flock.
# Without it, concurrent sessions interleave stop/start of the shared editor
# service and the last trap leaves it dead for every MCP client. Waiters end
# a holder whose Unity log stalled (scripts/lib/unity-editor-lock.sh).
# shellcheck source=lib/unity-editor-lock.sh
source "$ROOT/scripts/lib/unity-editor-lock.sh"
unity_lock_acquire run-unity-build

# --- 1. Warm-editor service -------------------------------------------------
# Stop first: the keep-warm editor otherwise trips the lock gate on every run
# and races the build over Library/ (3 h single-thread spin observed).
# Only the main checkout owns that service; a worktree build targets its own
# project copy and must not evict MCP sessions from the shared editor.
SERVICE_STOPPED=0
if [ "$PROJECT" = "$HOME/unity-sandbox/sandbox" ] && systemctl --user is-active --quiet "$SERVICE" 2>/dev/null; then
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

# --- 3. Build with heartbeat, stall watchdog + hard timeout ----------------
echo "run-unity-build: $METHOD (timeout ${TIMEOUT}s, stall ${UNITY_STALL_S}s, jobs $JOBS, log $LOG)"
# UNITY_BUILD_EXTRA_ARGS: optional extra editor args, word-split on purpose
# (e.g. "-buildOut /path/to/player" for SandboxBuilder.BuildDiabloZoneBuild).
# shellcheck disable=SC2086
unity_run_watched run-unity-build "$LOG" "$TIMEOUT" \
  "$EDITOR" -batchmode -nographics -quit -projectPath "$PROJECT" \
  -job-worker-count "$JOBS" -executeMethod "$METHOD" -logFile "$LOG" ${UNITY_BUILD_EXTRA_ARGS:-}
RC=$UNITY_RUN_RC
if [ "$UNITY_RUN_END" != done ]; then
  STRAGGLERS=$(live_holders || true)
  # shellcheck disable=SC2086
  [ -n "$STRAGGLERS" ] && kill -9 $STRAGGLERS 2>/dev/null || true
  if grep -a -F -q "$MARKER" "$LOG" 2>/dev/null; then
    # Known failure 2: Unity hangs in shutdown after a finished build.
    echo "run-unity-build: $UNITY_RUN_END after the success marker, outputs are written; treating as OK."
    RC=0
  else
    echo "--- log tail ---" >&2
    tail -20 "$LOG" | cut -c1-160 >&2 || true
    echo "run-unity-build: $UNITY_RUN_END, exit 2 (safe to retry)." >&2
    exit 2
  fi
fi

# --- 4. Result ---------------------------------------------------------------
trap - EXIT
restore_service
# Known failure 3: the editor crashes by signal (e.g. SIGFPE, exit 136) while it shuts down
# after a finished build and the batchmode quit; the outputs are already written.
if [ "$RC" -ge 128 ] && grep -a -F -q "$MARKER" "$LOG" \
   && grep -a -F -q "Batchmode quit successfully invoked" "$LOG"; then
  echo "run-unity-build: WARNING Unity crashed in shutdown (exit $RC) after the success marker; treating as OK."
  RC=0
fi
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
