#!/usr/bin/env bash
# unity-accelerator.sh — Unity Accelerator als Docker-Daemon auf dieser Maschine.
#
#   bash scripts/unity-accelerator.sh up      # Container starten (persistentes
#                                             # Volume ~/.local/share/unity-accelerator)
#   bash scripts/unity-accelerator.sh status  # Dashboard + Cache-Port prüfen
#   bash scripts/unity-accelerator.sh down    # Container stoppen/entfernen
#
# Builds nutzen ihn per Env (unset = unverändert):
#   UNITY_ACCELERATOR=127.0.0.1:10080 UNITY_ACCELERATOR_NAMESPACE=unity-sandbox \
#     python3 scripts/measure-build.py --note accelerator
# (run-unity-build.sh hängt dann -cacheServerEndpoint/-cacheServerNamespacePrefix
# sowie Download/Upload-Flags an den Unity-Aufruf.)
set -euo pipefail
NAME=unity-accelerator
IMG=unitytechnologies/accelerator
DATA="$HOME/.local/share/unity-accelerator"
CACHE_PORT="${UNITY_ACCELERATOR_PORT:-10080}"
DASH_PORT="${UNITY_ACCELERATOR_DASH_PORT:-10081}"

cmd=${1:?usage: unity-accelerator.sh up|status|down}
case "$cmd" in
  up)
    mkdir -p "$DATA"
    if docker ps --format '{{.Names}}' | grep -qx "$NAME"; then
      echo "unity-accelerator: already running."
      exit 0
    fi
    docker rm -f "$NAME" >/dev/null 2>&1 || true
    docker run -d --name "$NAME" --restart unless-stopped \
      -p "127.0.0.1:$CACHE_PORT:10080" -p "127.0.0.1:$DASH_PORT:80" \
      -v "$DATA:/agent" "$IMG" >/dev/null
    echo "unity-accelerator: up (cache 127.0.0.1:$CACHE_PORT, dashboard 127.0.0.1:$DASH_PORT)."
    ;;
  status)
    docker ps --filter "name=$NAME" --format '{{.Names}} ({{.Status}})' || true
    curl -s -m5 -o /dev/null -w "dashboard: %{http_code}\n" "http://127.0.0.1:$DASH_PORT/"
    ;;
  down)
    docker rm -f "$NAME" >/dev/null 2>&1 || true
    echo "unity-accelerator: removed (data kept in $DATA)."
    ;;
  *) echo "usage: unity-accelerator.sh up|status|down" >&2; exit 2;;
esac
