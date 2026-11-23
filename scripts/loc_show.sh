#!/usr/bin/env bash
# loc_show.sh <file under Assets/Scripts>... — pending lint lines with their source line numbers.
cd "$(dirname "$0")/../sandbox/Assets/Scripts"
for f in "$@"; do
  echo "=== $f"
  grep -nE '"[^"]*[A-Za-z]{2,}[^"]*"' "$f" | grep -vE '^\s*[0-9]+:\s*(//|///)' | grep -vE 'Debug\.Log|new GameObject\(|\.Find\(|Resources\.Load|\[Header|\[Tooltip' | cut -c1-230
done
