#!/usr/bin/env bash
# test-unity-editor-lock.sh — stall handling of the machine-wide Unity editor
# lock (scripts/lib/unity-editor-lock.sh) with a fake editor, no Unity needed.
# Runs in an isolated TMPDIR lock; about 90 s. Exit 0 = all cases pass.
# Usage (repo root): bash scripts/test-unity-editor-lock.sh
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK=$(mktemp -d)
export TMPDIR="$WORK"
export UNITY_LOCK_FILE="$WORK/unity-sandbox-editor.lock"
export UNITY_STALL_S=4 UNITY_LOCK_POLL_S=1
unset UNITY_EDITOR_LOCK_HELD
FAILS=0
pass() { echo "PASS $1"; }
fail() { echo "FAIL $1"; FAILS=$((FAILS + 1)); }
cleanup() {
  # Holders carry $WORK in their arguments; the test shell itself does not.
  local p
  for p in $(pgrep -f "holder .*$WORK" 2>/dev/null); do unity_kill_tree "$p"; done
  rm -rf "$WORK"
}
trap cleanup EXIT

# Fake editor: writes its log, optionally the marker, then hangs silently.
cat >"$WORK/fake-editor" <<'EOF'
#!/usr/bin/env bash
log=""
while [ $# -gt 0 ]; do [ "$1" = -logFile ] && log="$2"; shift; done
echo "fake editor start" >"$log"
[ -n "${FAKE_MARKER:-}" ] && echo "$FAKE_MARKER" >>"$log"
exec sleep 300
EOF
chmod +x "$WORK/fake-editor"

# shellcheck source=lib/unity-editor-lock.sh
source "$ROOT/scripts/lib/unity-editor-lock.sh"

# Start an orphaned holder: a process that keeps the lock descriptor after its
# parent shell exits, with an owner record. $1 = log, $2 = log age (s),
# $3 = 1 to record a pid whose cmdline lacks the log (reused pid).
start_orphan_holder() {
  local log="$1" age="$2" reused="${3:-0}"
  (
    exec 9>"$UNITY_LOCK_FILE"
    flock 9
    bash -c 'sleep 300; :' holder "$log" "$WORK" >/dev/null 2>&1 &
    local p=$!
    echo "x" >"$log"
    touch -d "-${age} seconds" "$log"
    if [ "$reused" = 1 ]; then
      sleep 300 9>&- >/dev/null 2>&1 &
      printf '%s\t%s\t%s\n' "$!" "$log" "$(date +%s)" >"$UNITY_LOCK_OWNER"
      echo "$p $!" >"$WORK/holder.pids"
    else
      unity_lock_owner "$p" "$log"
      echo "$p" >"$WORK/holder.pids"
    fi
  )
  sleep 0.5
}

waiter() { # $1 = seconds allowed; exit 0 when the lock was acquired
  timeout "$1" bash -c "source '$ROOT/scripts/lib/unity-editor-lock.sh'; unity_lock_acquire waiter" >"$WORK/waiter.out" 2>&1
}

# 1. Orphaned holder with a silent log: the waiter ends it and takes the lock.
start_orphan_holder "$WORK/stalled.log" 60
hp=$(cut -d' ' -f1 <"$WORK/holder.pids")
if waiter 30 && ! kill -0 "$hp" 2>/dev/null; then pass "waiter ends a stalled orphaned holder"; else fail "stalled orphan not reaped: $(cat "$WORK/waiter.out")"; fi

# 2. Holder with a fresh log: the waiter keeps queueing.
start_orphan_holder "$WORK/live.log" 0
hp=$(cut -d' ' -f1 <"$WORK/holder.pids")
( for _ in $(seq 1 12); do touch "$WORK/live.log"; sleep 1; done ) >/dev/null 2>&1 &
if ! waiter 8 && kill -0 "$hp" 2>/dev/null; then pass "waiter keeps an active holder"; else fail "active holder disturbed"; fi
unity_kill_tree "$hp"; sleep 1

# 3. Owner record pointing at an unrelated (reused) pid: never killed.
start_orphan_holder "$WORK/reused.log" 60 1
read -r hp other <"$WORK/holder.pids"
waiter 6 >/dev/null
if kill -0 "$other" 2>/dev/null; then pass "reused pid without the log path is left alone"; else fail "unrelated pid killed"; fi
unity_kill_tree "$hp"; unity_kill_tree "$other"; sleep 1
rm -f "$UNITY_LOCK_OWNER"

# 4. run-unity-build.sh: its own stalled run ends with exit 2 and frees the lock.
cd "$ROOT"
t0=$SECONDS
UNITY_EDITOR="$WORK/fake-editor" bash scripts/run-unity-build.sh Fake.Method "never printed" 600 "$WORK/build.log" >"$WORK/b.out" 2>&1
rc=$?
if [ "$rc" -eq 2 ] && [ $((SECONDS - t0)) -lt 60 ] && waiter 5; then pass "build stall -> exit 2, lock free"; else fail "build stall rc=$rc after $((SECONDS - t0))s: $(tail -3 "$WORK/b.out")"; fi

# 5. Stall after the success marker (shutdown hang): OK.
FAKE_MARKER="Fake build result: Succeeded" UNITY_EDITOR="$WORK/fake-editor" \
  bash scripts/run-unity-build.sh Fake.Method "Fake build result: Succeeded" 600 "$WORK/build2.log" >"$WORK/b2.out" 2>&1
rc=$?
if [ "$rc" -eq 0 ]; then pass "stall after the marker counts as OK"; else fail "marker stall rc=$rc: $(tail -3 "$WORK/b2.out")"; fi

# 6. run-unity-tests.sh: a stalled platform ends the whole run with exit 2.
t0=$SECONDS
UNITY_EDITOR="$WORK/fake-editor" UNITY_TESTS_SKIP_WARMUP=1 PLATFORMS="EditMode PlayMode" \
  bash scripts/run-unity-tests.sh >"$WORK/t.out" 2>&1
rc=$?
if [ "$rc" -eq 2 ] && ! grep -q "Running PlayMode" "$WORK/t.out" && waiter 5; then pass "test stall -> exit 2 before the next platform, lock free"; else fail "test stall rc=$rc after $((SECONDS - t0))s: $(tail -3 "$WORK/t.out")"; fi

echo "test-unity-editor-lock: $FAILS failure(s)"
[ "$FAILS" -eq 0 ]
