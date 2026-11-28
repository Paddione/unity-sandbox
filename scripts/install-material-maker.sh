#!/usr/bin/env bash
set -euo pipefail

VERSION="${MATERIAL_MAKER_VERSION:-1.8}"
TAR_URL="${MATERIAL_MAKER_URL:-https://github.com/RodZill4/material-maker/releases/download/${VERSION}/material_maker_${VERSION//./_}_linux.tar.gz}"
INSTALL_ROOT="${MATERIAL_MAKER_ROOT:-$HOME/.local/material-maker/${VERSION}}"
BIN_LINK="${MATERIAL_MAKER_BIN:-$HOME/.local/bin/material-maker}"

mkdir -p "$INSTALL_ROOT" "$(dirname "$BIN_LINK")"

find_bin() {
  find "$INSTALL_ROOT" -maxdepth 3 -type f -executable \( -name 'material_maker*' -o -name 'material-maker*' \) ! -name '*.sh' ! -name '*.so*' 2>/dev/null | head -n 1 || true
}

exe="$(find_bin)"
if [[ -z "$exe" ]]; then
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT
  echo "Downloading Material Maker ${VERSION} from ${TAR_URL}..."
  curl -fsSL "$TAR_URL" -o "$tmp/material-maker.tar.gz"
  tar -xzf "$tmp/material-maker.tar.gz" -C "$INSTALL_ROOT"
  exe="$(find_bin)"
fi

if [[ -z "$exe" ]]; then
  echo "Material Maker executable not found under $INSTALL_ROOT" >&2
  exit 1
fi

chmod +x "$exe"
ln -sfn "$exe" "$BIN_LINK"
echo "Material Maker installed: $BIN_LINK -> $exe"
