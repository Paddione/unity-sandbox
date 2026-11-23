#!/usr/bin/env bash
# publish-server-image.sh — container image for the Cinderreach dedicated server.
#
# Builds the Linux Server player (SandboxBuilder.BuildDiabloZoneLinuxServer, skip with
# SERVER_SKIP_BUILD=1), wraps it with docker/server/Dockerfile and pushes
# ghcr.io/paddione/cinderreach-server:<commit> (+ :latest). Prints the pushed digest, which the
# dotfiles manifest apps/cinderreach-server/k8s/deployment.yaml pins.
#
# Env: IMAGE (default ghcr.io/paddione/cinderreach-server), BUILD_DIR (default
#      <repo>/builds/cinderreach-server), SERVER_SKIP_BUILD=1, PUSH=0 (build the image only).
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
IMAGE="${IMAGE:-ghcr.io/paddione/cinderreach-server}"
BUILD_DIR="${BUILD_DIR:-$REPO/builds/cinderreach-server}"
COMMIT="$(git -C "$REPO" rev-parse --short HEAD)"

if [[ "${SERVER_SKIP_BUILD:-0}" != 1 ]]; then
  UNITY_BUILD_EXTRA_ARGS="-buildOut $BUILD_DIR/cinderreach-server" bash "$REPO/scripts/run-unity-build.sh" \
    SandboxBuilder.BuildDiabloZoneLinuxServer "DiabloZone server build result: Succeeded" 1500
fi
[[ -x "$BUILD_DIR/cinderreach-server" ]] || { echo "publish-server-image: no server build at $BUILD_DIR" >&2; exit 1; }

ctx="$(mktemp -d)"
trap 'rm -rf "$ctx"' EXIT
cp "$REPO/docker/server/Dockerfile" "$ctx/Dockerfile"
cp -r "$BUILD_DIR" "$ctx/server"
rm -rf "$ctx/server/"*_BurstDebugInformation_DoNotShip
echo "$COMMIT" > "$ctx/server/BUILD_COMMIT"

docker build --pull -t "$IMAGE:$COMMIT" -t "$IMAGE:latest" "$ctx"
if [[ "${PUSH:-1}" == 0 ]]; then
  echo "publish-server-image: built $IMAGE:$COMMIT (not pushed)"
  exit 0
fi
docker push "$IMAGE:$COMMIT"
docker push "$IMAGE:latest"
digest="$(docker inspect --format '{{index .RepoDigests 0}}' "$IMAGE:$COMMIT")"
echo "publish-server-image: OK $digest"
