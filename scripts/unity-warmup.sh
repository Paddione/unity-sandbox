#!/usr/bin/env bash
# unity-warmup.sh [log_dir] — compile session(s) before tests and player builds.
#
# Runs SandboxBuilder.CompileCheck through run-unity-build.sh. That session
# compiles changed scripts outside the test/build session and calls
# LibraryRepair.EnsureScriptsBound: runtime scripts whose MonoScript lost its
# class binding (seen after copying Library from the main checkout into a
# worktree) are reimported, which only takes effect in the next session. The
# check therefore repeats until a session reports "scripts bound" (at most 2).
#
# Exit 0 = bound, 1 = compile failed or still unbound after 2 sessions,
# 2/3 = passed through from run-unity-build.sh (hang / project busy).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${1:-$ROOT/artifacts}"
mkdir -p "$OUT"
for i in 1 2; do
  log="$OUT/warmup-$i.log"
  bash "$ROOT/scripts/run-unity-build.sh" SandboxBuilder.CompileCheck \
    "[SandboxBuilder] Compile check OK" 900 "$log"
  if grep -aq "\[LibraryRepair\] scripts bound" "$log"; then
    echo "unity-warmup: runtime scripts bound (session $i)"
    exit 0
  fi
  grep -a "\[LibraryRepair\]" "$log" | head -n 1 || true
done
echo "unity-warmup: runtime scripts still unbound after 2 sessions, see $OUT/warmup-2.log" >&2
exit 1
