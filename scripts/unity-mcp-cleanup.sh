#!/usr/bin/env bash
# unity-mcp-cleanup.sh — remove stale Unity MCP sockets/connection records.
#
# Every Editor start creates /tmp/unity-mcp-<hash>-<pid> plus
# ~/.unity/mcp/connections/bridge-<hash>-<pid>.json, and neither is removed
# when the Editor dies (200 stale sockets observed 2026-11-18 after the
# editor service flapped). Dead entries make relay discovery noisy; live
# entries (Editor PID alive) are never touched.
#
# Usage: bash scripts/unity-mcp-cleanup.sh [--dry-run]
set -euo pipefail

DRY=0
[ "${1:-}" = "--dry-run" ] && DRY=1

removed=0
for sock in /tmp/unity-mcp-*; do
  [ -e "$sock" ] || continue
  pid="${sock##*-}"
  case "$pid" in (*[!0-9]*|"") continue ;; esac
  if ! kill -0 "$pid" 2>/dev/null; then
    echo "stale socket (editor pid $pid dead): $sock"
    [ "$DRY" = 0 ] && rm -f "$sock"
    removed=$((removed + 1))
  fi
done
for rec in "$HOME/.unity/mcp/connections"/bridge-*.json; do
  [ -e "$rec" ] || continue
  pid="${rec##*-}"; pid="${pid%.json}"
  case "$pid" in (*[!0-9]*|"") continue ;; esac
  if ! kill -0 "$pid" 2>/dev/null; then
    echo "stale connection record (editor pid $pid dead): $rec"
    [ "$DRY" = 0 ] && rm -f "$rec"
    removed=$((removed + 1))
  fi
done
echo "unity-mcp-cleanup: $removed stale entr$( [ "$removed" = 1 ] && echo y || echo ies) $( [ "$DRY" = 1 ] && echo "would be removed" || echo removed)."
