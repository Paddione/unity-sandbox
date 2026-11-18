#!/usr/bin/env bash
# run-unity-tests.sh — hardened Unity test wrapper (track P2).
#
# Discipline transferred from run-unity-build.sh:
#  - stops unity-sandbox-editor.service for the run and restarts it after
#    (trap-guarded); a second editor otherwise trips the project lock and
#    `unity test` refuses projects that are open in an Editor; only a run
#    in the main checkout manages that shared service — worktree runs
#    leave it (and every MCP session on it) alone;
#  - serializes all builds/tests machine-wide via flock on
#    /tmp/unity-sandbox-editor.lock (shared with run-unity-build.sh), so
#    concurrent sessions queue instead of leaving the shared editor dead;
#  - refuses to start when another Unity holds this project (exit 3);
#    only a stale lockfile (no live holder) is removed;
#  - hard timeout per platform (default 1500 s): exit 2 on hang (timeout 124),
#    exit 1 on test/process failure — a hang is not a test failure;
#  - heartbeat line every 60 s so a stuck run looks different from a slow one.
#
# Exit semantics (live CLI table, verified 2026-11-17):
#   0 = all suites passed; 1 = tests/process failed (do not blind-retry a
#   failing suite — fix the code); 2 = infra hang (safe to retry);
#   3 = project lock held (safe to retry after the holder exits).
# With `unity test` the same split is exit 8 (TESTS_FAILED, no retry) versus
# any other non-zero code (run never completed, safe to retry).
#
# Filter-first convention: run the cheapest suite that can fail for the change.
#   PLATFORMS="EditMode" bash scripts/run-unity-tests.sh   # one suite only
#
# Usage (from the repo root): bash scripts/run-unity-tests.sh
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
version=$(awk '/^m_EditorVersion:/{print $2; exit}' sandbox/ProjectSettings/ProjectVersion.txt)
editor="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$version/Editor/Unity}"
[[ -n "$version" && -x "$editor" ]] || { echo "Unity editor missing: $editor" >&2; exit 1; }
project="$PWD/sandbox"
artifacts="$PWD/artifacts"
mkdir -p "$artifacts"
TIMEOUT=${UNITY_TEST_TIMEOUT_S:-1500}
SERVICE=unity-sandbox-editor.service
LOCKFILE="$project/Temp/UnityLockfile"

live_holders() {
  local holders
  holders=$(pgrep -f "Unity.*-projectPath $project" || true)
  holders=$(echo "$holders" | grep -v -e "^$$\$" -e "^$PPID\$" || true)
  echo "$holders"
}

# Machine-wide editor lock: queue behind any build/test (see header).
exec 9>"${TMPDIR:-/tmp}/unity-sandbox-editor.lock"
echo "run-unity-tests: waiting for the machine-wide editor lock..."
flock 9
echo "run-unity-tests: editor lock acquired."

SERVICE_STOPPED=0
if [ "$project" = "$HOME/unity-sandbox/sandbox" ] && systemctl --user is-active --quiet "$SERVICE" 2>/dev/null; then
  echo "run-unity-tests: stopping $SERVICE for the run."
  systemctl --user stop "$SERVICE" || true
  SERVICE_STOPPED=1
  for _ in $(seq 1 60); do
    [ -z "$(live_holders)" ] && break
    sleep 1
  done
fi
restore_service() {
  if [ "$SERVICE_STOPPED" = 1 ]; then
    echo "run-unity-tests: restarting $SERVICE."
    # A SIGKILLed stop leaves the unit in failed state, which plain start
    # cannot revive (observed 2026-11-17) — reset first.
    systemctl --user reset-failed "$SERVICE" 2>/dev/null || true
    systemctl --user start "$SERVICE" || true
  fi
  rm -f "$LOCKFILE"
}
trap restore_service EXIT

if [ -e "$LOCKFILE" ]; then
  if [ -n "$(live_holders)" ]; then
    echo "run-unity-tests: project locked by a live Unity holder; refusing (exit 3)." >&2
    exit 3
  fi
  echo "run-unity-tests: removing stale lockfile."
  rm -f "$LOCKFILE"
fi

heartbeat() {
  local log="$1" start="$2"
  while true; do
    sleep 60
    echo "run-unity-tests: still running ($(( $(date +%s) - start ))s): $(tail -n 1 "$log" 2>/dev/null | cut -c1-160)"
  done
}

failed=0
# shellcheck disable=SC2206
platforms=(${PLATFORMS:-EditMode PlayMode})
for platform in "${platforms[@]}"; do
  result="$artifacts/$platform.xml"
  log="$artifacts/$platform.log"
  rm -f "$result"
  echo "Running $platform with Unity $version"
  start=$(date +%s)
  heartbeat "$log" "$start" &
  hb=$!
  rc=0
  # -quit would kill -runTests (dev-journal Q5): never pass it here.
  timeout "$TIMEOUT" "$editor" -batchmode -nographics -projectPath "$project" -runTests \
      -testPlatform "$platform" -testResults "$result" -logFile "$log" || rc=$?
  kill "$hb" 2>/dev/null || true
  if [ "$rc" -eq 124 ]; then
    echo "$platform hung past ${TIMEOUT}s — infra hang (exit 2), safe to retry" >&2
    tail -n 60 "$log" >&2 || true
    failed=2
    continue
  elif [ "$rc" -ne 0 ]; then
    echo "$platform editor/test process failed (exit $rc)" >&2
    tail -n 60 "$log" >&2 || true
    failed=1
  fi
  if ! python3 - "$result" "$platform" <<'PY'
import os
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

path, platform = Path(sys.argv[1]), sys.argv[2]
if not path.is_file():
    raise SystemExit(f"{platform}: test result XML missing")
root = ET.parse(path).getroot()
total = int(root.get("total", "0"))
failed = int(root.get("failed", "0"))
passed = int(root.get("passed", "0"))
summary = f"{platform}: {passed}/{total} passed, {failed} failed ({root.get('result')})"
print(summary)
if os.environ.get("GITHUB_STEP_SUMMARY"):
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as stream:
        stream.write(summary + "\n\n")
if root.get("result") != "Passed" or total < 1 or failed:
    raise SystemExit(1)
PY
  then
    [ "$failed" = 2 ] || failed=1
  fi
done
exit "$failed"
