#!/usr/bin/env bash
# build-installer.sh — package the Windows player build into a Cinderreach setup .exe.
#
# Needs a finished Windows build (SandboxBuilder.BuildDiabloZoneWindows → builds/cinderreach-windows)
# and Inno Setup 6 on the Windows host (winget install JRSoftware.InnoSetup --scope user).
# Output: builds/cinderreach-installer/Cinderreach-Setup-<bundleVersion>-<commit>.exe,
# copied to INSTALLER_DEST (Windows-visible folder) when set.
#
# Env: BUILD_DIR (default <repo>/builds/cinderreach-windows), ISCC (path to ISCC.exe),
#      INSTALLER_DEST (optional copy target, e.g. /mnt/c/Users/<you>/Desktop).
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-$REPO/builds/cinderreach-windows}"
OUT_DIR="$REPO/builds/cinderreach-installer"
ISS="$REPO/scripts/installer/cinderreach.iss"
CREST="$REPO/sandbox/Assets/AddressableAssets/MenuArt/CrestCinderreach.png"

if [[ -z "${ISCC:-}" ]]; then
  win_local="$(wslpath "$(cmd.exe /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')")"
  for cand in "$win_local/Programs/Inno Setup 6/ISCC.exe" "/mnt/c/Program Files (x86)/Inno Setup 6/ISCC.exe" \
              "/mnt/c/Program Files/Inno Setup 6/ISCC.exe"; do
    [[ -x "$cand" ]] && ISCC="$cand" && break
  done
fi
[[ -n "${ISCC:-}" && -x "$ISCC" ]] || { echo "build-installer: ISCC.exe not found (install Inno Setup 6 or set ISCC)" >&2; exit 1; }
[[ -f "$BUILD_DIR/cinderreach.exe" ]] || { echo "build-installer: no Windows build at $BUILD_DIR" >&2; exit 1; }

version="$(sed -n 's/^  bundleVersion: //p' "$REPO/sandbox/ProjectSettings/ProjectSettings.asset" | head -1)"
commit="$(git -C "$REPO" rev-parse --short HEAD)"
if [[ -f "$BUILD_DIR/BUILD_INFO.json" ]]; then
  built="$(sed -n 's/.*"commit":"\([0-9a-f]*\)".*/\1/p' "$BUILD_DIR/BUILD_INFO.json")"
  [[ -n "$built" ]] && commit="$built"
fi

# Windows-side staging: ISCC reads local NTFS paths fastest and avoids UNC quirks.
win_tmp="$(wslpath "$(cmd.exe /c 'echo %TEMP%' 2>/dev/null | tr -d '\r')")/cinderreach-installer"
rm -rf "$win_tmp"
mkdir -p "$win_tmp/src" "$win_tmp/out"
cp -r "$BUILD_DIR/." "$win_tmp/src/"
cp "$ISS" "$win_tmp/cinderreach.iss"

icon_args=()
if [[ -f "$CREST" ]]; then
  AGENT_HOST_PYTHON=1 python3 - "$CREST" "$win_tmp/Cinderreach.ico" <<'PY'
import sys
from PIL import Image
im = Image.open(sys.argv[1]).convert("RGBA")
side = max(im.size)
sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
sq.save(sys.argv[2], sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
PY
  icon_args=("/DIconFile=$(wslpath -w "$win_tmp/Cinderreach.ico")")
fi

echo "build-installer: Cinderreach $version ($commit) from $BUILD_DIR"
"$ISCC" /Q "/DSrcDir=$(wslpath -w "$win_tmp/src")" "/DOutDir=$(wslpath -w "$win_tmp/out")" \
  "/DAppVersion=$version" "/DCommit=$commit" "${icon_args[@]}" "$(wslpath -w "$win_tmp/cinderreach.iss")"

mkdir -p "$OUT_DIR"
setup="$(ls "$win_tmp/out"/Cinderreach-Setup-*.exe)"
cp "$setup" "$OUT_DIR/"
out="$OUT_DIR/$(basename "$setup")"
if [[ -n "${INSTALLER_DEST:-}" ]]; then
  mkdir -p "$INSTALLER_DEST"
  cp "$setup" "$INSTALLER_DEST/"
  out="$INSTALLER_DEST/$(basename "$setup")"
fi
rm -rf "$win_tmp"
echo "build-installer: OK $out ($(du -h "$out" | cut -f1))"
