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
#  - hard timeout per platform (default 1500 s) and a CPU-aware stall
#    watchdog (log silent for UNITY_STALL_S, default 600 s, AND the process
#    tree CPU-idle over the probe window): exit 2 on either, at once, so
#    the lock goes to the next queued run; exit 1 on test/process failure;
#    waiting runs end a stalled lock holder (scripts/lib/unity-editor-lock.sh);
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
#   TEST_FILTER="LayoutSeedTests" PLATFORMS=EditMode bash scripts/run-unity-tests.sh
# Failing tests (name, first Assets/ frame, message) and the slowest
# UNITY_TESTS_SLOWEST (default 5) tests are printed by scripts/unity_test_report.py.
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

# Machine-wide editor lock: queue behind any build/test (see header). Waiters
# end a holder whose Unity log stalled. The lock exports
# UNITY_EDITOR_LOCK_HELD=1, so nested run-unity-build.sh calls (warm-up) do
# not wait on it again.
# shellcheck source=lib/unity-editor-lock.sh
source "$PWD/scripts/lib/unity-editor-lock.sh"
unity_lock_acquire run-unity-tests

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

# Warm-up (scripts/unity-warmup.sh): compile outside the test session and
# rebind runtime scripts whose MonoScript lost its class (Editor/LibraryRepair.cs).
# Without it a worktree with a copied Library ran EditMode with every scene
# component null (14 scene tests red, 2026-11-18). Skip: UNITY_TESTS_SKIP_WARMUP=1.
if [ "${UNITY_TESTS_SKIP_WARMUP:-0}" != 1 ]; then
  bash "$PWD/scripts/unity-warmup.sh" "$artifacts" || exit 1
fi

# Filtered runs (iteration only; the gate runs unfiltered):
#   TEST_FILTER   -> -testFilter   (semicolon list or regex of full test names)
#   TEST_CATEGORY -> -testCategory (semicolon list)
# A filter that matches nothing in a platform is not a failure there.
filter_args=()
report_args=(--slowest "${UNITY_TESTS_SLOWEST:-5}")
summary_args=()
if [ -n "${TEST_FILTER:-}" ]; then filter_args+=(-testFilter "$TEST_FILTER"); fi
if [ -n "${TEST_CATEGORY:-}" ]; then filter_args+=(-testCategory "$TEST_CATEGORY"); fi
if [ "${#filter_args[@]}" -gt 0 ]; then
  report_args+=(--allow-empty)
  echo "run-unity-tests: filtered run (${filter_args[*]}) — not a full gate."
else
  # Versioned evidence for the project board (Goal unity-projektstand-board, D2).
  summary_args+=(--append-summary "$PWD/docs/test-runs.jsonl")
fi

failed=0
# shellcheck disable=SC2206
platforms=(${PLATFORMS:-EditMode PlayMode})
for platform in "${platforms[@]}"; do
  result="$artifacts/$platform.xml"
  log="$artifacts/$platform.log"
  rm -f "$result"
  echo "Running $platform with Unity $version"
  # -quit would kill -runTests (dev-journal Q5): never pass it here.
  unity_run_watched run-unity-tests "$log" "$TIMEOUT" \
      "$editor" -batchmode -nographics -projectPath "$project" -runTests \
      -testPlatform "$platform" -testResults "$result" -logFile "$log" \
      "${filter_args[@]}"
  rc=$UNITY_RUN_RC
  if [ "$UNITY_RUN_END" != done ]; then
    # Stall or timeout: infra hang. Stop here so the lock goes to the next run.
    echo "$platform $UNITY_RUN_END — infra hang (exit 2), safe to retry" >&2
    tail -n 60 "$log" >&2 || true
    exit 2
  elif [ "$rc" -ge 128 ] && [ -s "$result" ]; then
    # Known failure: the editor crashes by signal (e.g. SIGFPE, exit 136) while it shuts
    # down after the run wrote its results. The report below still judges every test.
    echo "run-unity-tests: WARNING $platform editor crashed in shutdown (exit $rc) after writing $result; judging the results."
  elif [ "$rc" -ne 0 ]; then
    echo "$platform editor/test process failed (exit $rc)" >&2
    tail -n 60 "$log" >&2 || true
    failed=1
  fi
  if ! python3 "$PWD/scripts/unity_test_report.py" "$result" "$platform" "${report_args[@]}" "${summary_args[@]}"
  then
    [ "$failed" = 2 ] || failed=1
  fi
done
exit "$failed"
