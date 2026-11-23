#!/usr/bin/env bash
# account-smoke.sh — live account check against the linked UGS project
# (cinderreach-accounts S2). Builds the Linux player (skip: AUTOPLAY_SKIP_BUILD=1),
# then runs it twice on one throwaway auth profile:
#   1. -account-smoke create   guest -> create account (same id) -> display name
#   2. -account-smoke restore  restored session -> wrong/right sign-in -> sign-out -> delete account
# A fresh random test user per run; credentials only via environment.
# Exit 0 = every ACCOUNT_SMOKE line ok in both runs.
# Usage (repo root): bash scripts/account-smoke.sh [out_dir]
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${1:-$ROOT/artifacts/account-smoke}"
mkdir -p "$OUT"
PLAYER="$ROOT/builds/autoplay-player/cinderreach"
if [ "${AUTOPLAY_SKIP_BUILD:-0}" != 1 ]; then
  UNITY_BUILD_EXTRA_ARGS="-buildOut $PLAYER" bash "$ROOT/scripts/run-unity-build.sh" \
    SandboxBuilder.BuildDiabloZoneBuild "DiabloZone build result: Succeeded" 1500 "$OUT/build.log"
fi
[ -x "$PLAYER" ] || { echo "account-smoke: player missing: $PLAYER" >&2; exit 1; }

stamp="$(date +%s)"
export CR_SMOKE_USER="crsmoke_${stamp}"
export CR_SMOKE_PASS="Smk-${stamp}-Aa1!"
PROFILE="smoke${stamp}"
rc=0
for mode in create restore; do
  timeout --kill-after=15 180 "$PLAYER" -batchmode -nographics -profile "$PROFILE" \
    -account-smoke "$mode" -save-file account-smoke.json -logFile "$OUT/$mode.log" || rc=1
  grep "ACCOUNT_SMOKE" "$OUT/$mode.log" || { echo "account-smoke: no verdict in $mode" >&2; rc=1; }
  grep -q "ACCOUNT_SMOKE DONE mode=$mode failures=0" "$OUT/$mode.log" || rc=1
done
echo "account-smoke: user=$CR_SMOKE_USER profile=$PROFILE -> $rc (logs $OUT)"
exit $rc
