#!/usr/bin/env bash
# Render editor and game-camera references for the checked-in DiabloZone.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

project="$PWD/sandbox"
mkdir -p artifacts
out=$(mktemp -d "$PWD/artifacts/scene-captures.XXXXXX")
version=$(awk '/^m_EditorVersion:/{print $2; exit}' "$project/ProjectSettings/ProjectVersion.txt")
editor="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$version/Editor/Unity}"
cli="${UNITY_CLI:-$HOME/.local/bin/unity}"

if [[ ! -S /tmp/.X11-unix/X0 || ! -c /dev/dxg ]]; then
  echo "Scene captures require the WSLg GPU display and /dev/dxg." >&2
  exit 1
fi

exec 9>"$PWD/artifacts/scene-capture.lock"
flock -n 9
editor_lock="$project/Temp/UnityLockfile"
if [[ -f "$editor_lock" ]] || pgrep -f "[U]nity -project[Pp]ath $project([[:space:]]|$)" >/dev/null; then
  echo "The capture project is already open or locked." >&2
  exit 3
fi

# Unity updates these settings on graphical startup. Keep the input checkout intact.
settings="$project/ProjectSettings"
cp "$settings/PackageManagerSettings.asset" "$settings/ProjectSettings.asset" "$out/"
export DISPLAY=:0 GALLIUM_DRIVER=d3d12
runtime_dir="/run/user/$(id -u)"
if [[ -S "$runtime_dir/bus" ]]; then
  export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-$runtime_dir}"
  export DBUS_SESSION_BUS_ADDRESS="${DBUS_SESSION_BUS_ADDRESS:-unix:path=$runtime_dir/bus}"
fi
"$editor" -projectPath "$project" -force-glcore -logFile "$out/editor.log" 9>&- &
editor_pid=$!
cleanup() {
  if kill -0 "$editor_pid" 2>/dev/null; then
    "$cli" --non-interactive close "$project" --timeout 45 --force || kill "$editor_pid"
    wait "$editor_pid" || true
  fi
  if [[ -f "$editor_lock" ]]; then
    flock -n "$editor_lock" mv -- "$editor_lock" "$out/editor.lock"
  fi
  cp "$out/PackageManagerSettings.asset" "$out/ProjectSettings.asset" "$settings/"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

"$cli" status --project-path "$project" --until-ready --timeout 480
run() {
  "$cli" --non-interactive command --project-path "$project" "$@"
}
run scene open --path Scenes/DiabloZone.unity
run script eval --code '
var gpu = UnityEngine.SystemInfo.graphicsDeviceName;
if (!gpu.StartsWith("D3D12") || gpu.Contains("Basic Render") || gpu.Contains("WARP"))
    throw new System.InvalidOperationException("Expected a WSLg GPU, got " + gpu);
var view = UnityEditor.SceneView.lastActiveSceneView
    ?? UnityEditor.EditorWindow.GetWindow<UnityEditor.SceneView>();
view.LookAtDirect(new UnityEngine.Vector3(0, 0, 3), UnityEngine.Quaternion.Euler(65, 12, 0), 20f);
view.Repaint();
var camera = UnityEngine.Camera.main;
if (camera == null)
    throw new System.InvalidOperationException("DiabloZone has no main camera");
var target = new UnityEngine.Vector3(0, 0, 6);
camera.transform.rotation = UnityEngine.Quaternion.LookRotation(target - camera.transform.position);
return gpu;
' | tee "$out/renderer.txt"

run capture scene_view --source camera --width 1280 --height 720 --output-path "$out/scene-view.png"
run capture game_view --source camera --width 1280 --height 720 --output-path "$out/game-camera.png"
for file in "$out/scene-view.png" "$out/game-camera.png"; do
  test -s "$file"
  LC_ALL=C file "$file" | grep -F 'PNG image data, 1280 x 720'
done
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'directory=%s\n' "$out" >> "$GITHUB_OUTPUT"
fi
