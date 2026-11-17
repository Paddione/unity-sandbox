#!/usr/bin/env bash
# ugs-relay-probe.sh — checks that the linked UGS project serves Relay.
# Same flow as the SDK: anonymous sign-in -> Relay allocation -> join code.
# The project id comes from sandbox/ProjectSettings/ProjectSettings.asset
# (cloudProjectId); override with UGS_PROJECT_ID, environment with
# UGS_ENVIRONMENT (default production). Exit 0 = join code issued.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
project="${UGS_PROJECT_ID:-$(awk '/cloudProjectId:/{print $2; exit}' sandbox/ProjectSettings/ProjectSettings.asset)}"
env_name="${UGS_ENVIRONMENT:-production}"
[[ -n "$project" ]] || { echo "ugs-relay-probe: no cloudProjectId (project not linked)" >&2; exit 1; }
json() { python3 -c "import json,sys; d=json.load(sys.stdin); print($1)"; }
token=$(curl -sf -X POST https://player-auth.services.api.unity.com/v1/authentication/anonymous \
  -H "ProjectId: $project" -H "UnityEnvironment: $env_name" -H "Content-Type: application/json" -d '{}' \
  | json "d['idToken']") || { echo "ugs-relay-probe: anonymous sign-in failed" >&2; exit 1; }
alloc=$(curl -sf -X POST https://relay-allocations.services.api.unity.com/v1/allocate \
  -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{"maxConnections":3}' \
  | json "d['data']['allocation']['allocationId']") || { echo "ugs-relay-probe: allocation failed" >&2; exit 1; }
code=$(curl -sf -X POST https://relay-allocations.services.api.unity.com/v1/joincode \
  -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d "{\"allocationId\":\"$alloc\"}" \
  | json "d['data']['joinCode']") || { echo "ugs-relay-probe: join code failed" >&2; exit 1; }
echo "ugs-relay-probe: OK project=$project env=$env_name allocation=$alloc joinCode=$code"
