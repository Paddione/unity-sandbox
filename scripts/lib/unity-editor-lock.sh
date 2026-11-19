# shellcheck shell=bash
# unity-editor-lock.sh — machine-wide Unity editor lock with stall handling.
# Sourced by run-unity-build.sh and run-unity-tests.sh.
#
# All editor-owning runs (builds + tests, any checkout) queue on one flock
# (${TMPDIR:-/tmp}/unity-sandbox-editor.lock). The run that holds it records
# its Unity process and log in "<lock>.owner".
#
# Stall rule: a Unity run whose log has been silent for UNITY_STALL_S seconds
# (default 600) is a stall *candidate*. The verdict is CPU-aware (2026-11-19:
# a healthy EditMode suite can go log-silent for minutes under machine
# contention, while a frozen run sits at ~0 % CPU — silence alone cannot tell
# them apart). A silent AND CPU-idle tree is stuck; a silent but CPU-busy
# tree is slow, not stuck, and gets a fresh stall window (bounded by the
# caller's hard timeout). Two sides enforce it:
#  - the holder watches its own log (unity_run_watched) and ends the run with
#    exit 2, which releases the lock at once;
#  - waiters poll the lock every UNITY_LOCK_POLL_S seconds (default 30) and end
#    the recorded holder's process tree when it stalled. This covers an orphaned
#    holder whose wrapper died while Unity kept the lock descriptor open.
# The next waiter then takes the lock.
#
# Evidence discipline: every stall verdict carries its CPU reading
# (+<cpu>s over the last <probe>s); an unreadable tree (no /proc, raced
# exit) keeps the legacy silence-only verdict instead of blocking forever.
#
# Knobs: UNITY_STALL_S (600) — silence threshold. Healthy phases stay far
# below it (measured 2026-11-19: EditMode 344 tests ≈ 13 s, PlayMode 220
# tests ≈ 94 s of test execution); UNITY_STALL_CPU_PROBE_S (15) — CPU
# sampling window; UNITY_STALL_CPU_MIN_S (3) — minimum tree CPU seconds in
# one probe window to count as alive.

UNITY_LOCK_FILE="${UNITY_LOCK_FILE:-${TMPDIR:-/tmp}/unity-sandbox-editor.lock}"
UNITY_LOCK_OWNER="$UNITY_LOCK_FILE.owner"
UNITY_STALL_S="${UNITY_STALL_S:-600}"
UNITY_STALL_CPU_PROBE_S="${UNITY_STALL_CPU_PROBE_S:-15}"
UNITY_STALL_CPU_MIN_S="${UNITY_STALL_CPU_MIN_S:-3}"
UNITY_LOCK_POLL_S="${UNITY_LOCK_POLL_S:-30}"

# unity_lock_acquire <caller>: queue for the lock (no-op when the caller's
# parent already holds it, UNITY_EDITOR_LOCK_HELD=1: re-locking from a child
# opens a new file description and would wait on its own parent forever).
unity_lock_acquire() {
  if [ "${UNITY_EDITOR_LOCK_HELD:-0}" = 1 ]; then return 0; fi
  exec 9>"$UNITY_LOCK_FILE"
  echo "$1: waiting for the machine-wide editor lock..."
  until flock -w "$UNITY_LOCK_POLL_S" 9; do
    unity_lock_reap_stale "$1"
  done
  echo "$1: editor lock acquired."
  export UNITY_EDITOR_LOCK_HELD=1
}

# unity_lock_owner <pid> <log>: record the running Unity process of the holder.
unity_lock_owner() {
  local tmp="$UNITY_LOCK_OWNER.$$"
  printf '%s\t%s\t%s\n' "$1" "$2" "$(date +%s)" >"$tmp" && mv -f "$tmp" "$UNITY_LOCK_OWNER"
}

# unity_lock_owner_clear <pid>: drop the record if it still names this pid.
unity_lock_owner_clear() {
  local pid=""
  [ -f "$UNITY_LOCK_OWNER" ] && IFS=$'\t' read -r pid _ _ <"$UNITY_LOCK_OWNER"
  if [ "$pid" = "$1" ]; then rm -f "$UNITY_LOCK_OWNER"; fi
  return 0
}

# unity_log_silence <log> <since_epoch>: seconds since the log last changed
# (a missing log counts from since_epoch).
unity_log_silence() {
  local mt
  mt=$(stat -c %Y "$1" 2>/dev/null || echo "$2")
  echo $(( $(date +%s) - mt ))
}

unity_descendants() {
  local c
  for c in $(pgrep -P "$1" 2>/dev/null); do
    echo "$c"
    unity_descendants "$c"
  done
}

# unity_tree_cpu_s <pid>: cumulative user+sys CPU seconds (integer) of the
# process tree rooted at pid, via /proc. Prints nothing when the tree is
# unreadable (no /proc, raced exit) — callers treat that as unknown and keep
# the legacy silence-only verdict. Field parsing skips the comm field (which
# may contain spaces) by cutting everything up to the last ')'.
unity_tree_cpu_s() {
  local hz p line t total=0
  [ -r "/proc/$1/stat" ] || return 0
  hz=$(getconf CLK_TCK 2>/dev/null || echo 100)
  for p in "$1" $(unity_descendants "$1"); do
    line=$(sed 's/^.*) //' "/proc/$p/stat" 2>/dev/null) || continue
    t=$(awk '{print $12+$13}' <<<"$line" 2>/dev/null) || continue
    total=$((total + t))
  done
  echo $((total / hz))
}

# unity_stall_verdict <pid>: CPU-aware stall verdict for a log-silent tree.
# Sets UNITY_STALL_VERDICT (spared|stalled|unknown) and UNITY_STALL_CPU_NOTE
# (e.g. "+7s/15s"). First call only records a CPU mark and spares (the probe
# window has not elapsed yet); once a full UNITY_STALL_CPU_PROBE_S window
# passed, a tree that burned >= UNITY_STALL_CPU_MIN_S is spared, an idle one
# is stalled. State persists across calls in UNITY_STALL_CPU_MARK(_AT).
unity_stall_verdict() {
  local now cpu delta
  now=$(date +%s)
  cpu=$(unity_tree_cpu_s "$1")
  if [ -z "$cpu" ]; then
    UNITY_STALL_VERDICT=unknown
    UNITY_STALL_CPU_NOTE="cpu unknown"
    return 0
  fi
  if [ -z "${UNITY_STALL_CPU_MARK:-}" ]; then
    UNITY_STALL_CPU_MARK=$cpu
    UNITY_STALL_CPU_MARK_AT=$now
    UNITY_STALL_VERDICT=spared
    UNITY_STALL_CPU_NOTE="probing"
    return 0
  fi
  if [ "$((now - UNITY_STALL_CPU_MARK_AT))" -lt "$UNITY_STALL_CPU_PROBE_S" ]; then
    UNITY_STALL_VERDICT=spared
    UNITY_STALL_CPU_NOTE="probing"
    return 0
  fi
  delta=$((cpu - UNITY_STALL_CPU_MARK))
  UNITY_STALL_CPU_MARK=$cpu
  UNITY_STALL_CPU_MARK_AT=$now
  if [ "$delta" -ge "$UNITY_STALL_CPU_MIN_S" ]; then
    UNITY_STALL_VERDICT=spared
  else
    UNITY_STALL_VERDICT=stalled
  fi
  UNITY_STALL_CPU_NOTE="+${delta}s/${UNITY_STALL_CPU_PROBE_S}s"
}

# unity_kill_tree <pid>: TERM the process and all descendants, KILL after 10 s.
unity_kill_tree() {
  local all
  all="$1 $(unity_descendants "$1" | tr '\n' ' ')"
  # shellcheck disable=SC2086
  kill -TERM $all 2>/dev/null || true
  for _ in $(seq 1 10); do
    kill -0 "$1" 2>/dev/null || break
    sleep 1
  done
  # shellcheck disable=SC2086
  kill -KILL $all 2>/dev/null || true
}

# unity_lock_reap_stale <caller>: end the recorded holder when it stalled.
unity_lock_reap_stale() {
  [ -f "$UNITY_LOCK_OWNER" ] || return 0
  local pid="" log="" since="" silent cpu1="" cpu2="" note=""
  IFS=$'\t' read -r pid log since <"$UNITY_LOCK_OWNER" || return 0
  [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null || return 0
  # Guard against a reused pid: the recorded process must carry its log path.
  tr '\0' ' ' <"/proc/$pid/cmdline" 2>/dev/null | grep -qF -- "$log" || return 0
  silent=$(unity_log_silence "$log" "${since:-0}")
  if [ "$silent" -lt "$UNITY_STALL_S" ]; then
    echo "$1: still queued, holder pid $pid active (log silent ${silent}s)"
    return 0
  fi
  # CPU-aware verdict: sample the tree, wait out one probe window, sample
  # again. A busy tree is slow — leave it; an idle one is stuck — end it.
  # Re-check the cmdline before killing: the pid may have been recycled
  # during the probe sleep.
  cpu1=$(unity_tree_cpu_s "$pid")
  if [ -n "$cpu1" ]; then
    sleep "$UNITY_STALL_CPU_PROBE_S"
    kill -0 "$pid" 2>/dev/null || return 0
    tr '\0' ' ' <"/proc/$pid/cmdline" 2>/dev/null | grep -qF -- "$log" || return 0
    cpu2=$(unity_tree_cpu_s "$pid")
    if [ -n "$cpu2" ] && [ "$((cpu2 - cpu1))" -ge "$UNITY_STALL_CPU_MIN_S" ]; then
      echo "$1: holder pid $pid silent ${silent}s but CPU-busy (+$((cpu2 - cpu1))s/${UNITY_STALL_CPU_PROBE_S}s) — slow, not stuck; leaving it" >&2
      return 0
    fi
  fi
  if [ -n "$cpu1" ] && [ -n "$cpu2" ]; then
    note="+$((cpu2 - cpu1))s/${UNITY_STALL_CPU_PROBE_S}s"
  else
    note="cpu unknown"
  fi
  echo "$1: lock holder pid $pid stalled (log $log silent ${silent}s, tree CPU $note — idle), ending it" >&2
  unity_kill_tree "$pid"
  rm -f "$UNITY_LOCK_OWNER"
}

# unity_run_watched <caller> <log> <timeout_s> <cmd...>: run the Unity command
# in the background as lock owner, print a heartbeat every 60 s and end it on
# stall or timeout. Sets UNITY_RUN_RC (command exit code) and
# UNITY_RUN_END (done | stall | timeout).
unity_run_watched() {
  local caller="$1" log="$2" limit="$3"
  shift 3
  "$@" &
  local pid=$! start=$SECONDS since
  since=$(date +%s)
  unity_lock_owner "$pid" "$log"
  UNITY_RUN_END=done
  unset UNITY_STALL_CPU_MARK UNITY_STALL_CPU_MARK_AT
  local next_beat=60
  while kill -0 "$pid" 2>/dev/null; do
    sleep 5
    kill -0 "$pid" 2>/dev/null || break
    local elapsed=$((SECONDS - start)) silent
    silent=$(unity_log_silence "$log" "$since")
    if [ "$silent" -ge "$UNITY_STALL_S" ]; then
      # CPU-aware verdict: silent AND idle ends the run; silent but busy
      # grants a fresh stall window (bounded by the hard timeout below).
      unity_stall_verdict "$pid"
      case "$UNITY_STALL_VERDICT" in
        stalled|unknown)
          echo "$caller: STALL, log silent ${silent}s (>= ${UNITY_STALL_S}s), tree CPU $UNITY_STALL_CPU_NOTE — ending the run and releasing the lock" >&2
          UNITY_RUN_END=stall
          unity_kill_tree "$pid"
          break
          ;;
        spared)
          if [ "$UNITY_STALL_CPU_NOTE" != probing ]; then
            echo "$caller: log silent ${silent}s but tree CPU $UNITY_STALL_CPU_NOTE — slow, not stuck; fresh stall window" >&2
            since=$(date +%s)
          fi
          ;;
      esac
    fi
    if [ "$elapsed" -ge "$limit" ]; then
      echo "$caller: TIMEOUT after ${elapsed}s, ending the run" >&2
      UNITY_RUN_END=timeout
      unity_kill_tree "$pid"
      break
    fi
    if [ "$elapsed" -ge "$next_beat" ]; then
      echo "$caller: heartbeat ${elapsed}s, log silent ${silent}s: $(tail -n 1 "$log" 2>/dev/null | cut -c1-140)"
      next_beat=$((next_beat + 60))
    fi
  done
  UNITY_RUN_RC=0
  wait "$pid" 2>/dev/null || UNITY_RUN_RC=$?
  # Ended by a waiter's stall reap (record removed, log silent past the limit).
  if [ "$UNITY_RUN_END" = done ] && [ "$UNITY_RUN_RC" -ne 0 ] \
     && [ "$(unity_log_silence "$log" "$since")" -ge "$UNITY_STALL_S" ]; then
    UNITY_RUN_END=stall
  fi
  unity_lock_owner_clear "$pid"
}
