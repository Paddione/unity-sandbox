#!/usr/bin/env bash
# Run both suites in the checkout using the host user's existing Unity activation.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
version=$(awk '/^m_EditorVersion:/{print $2; exit}' sandbox/ProjectSettings/ProjectVersion.txt)
editor="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$version/Editor/Unity}"
[[ -n "$version" && -x "$editor" ]] || { echo "Unity editor missing: $editor" >&2; exit 1; }
project="$PWD/sandbox"
artifacts="$PWD/artifacts"
mkdir -p "$artifacts"
failed=0
for platform in EditMode PlayMode; do
  result="$artifacts/$platform.xml"
  log="$artifacts/$platform.log"
  rm -f "$result"
  echo "Running $platform with Unity $version"
  if ! "$editor" -batchmode -nographics -projectPath "$project" -runTests \
      -testPlatform "$platform" -testResults "$result" -logFile "$log"; then
    echo "$platform editor/test process failed" >&2
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
    failed=1
  fi
done
exit "$failed"
