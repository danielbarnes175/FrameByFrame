#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
BUILD_SCRIPT="${SCRIPT_DIR}/release.sh"

usage() {
  cat <<'EOF'
Usage: ./scripts/publish-release.sh <version> [framework]

Builds the release ZIP, creates and pushes an annotated Git tag, and creates
a GitHub release with generated release notes.

Examples:
  ./scripts/publish-release.sh 0.1.0
  ./scripts/publish-release.sh v0.1.0 net8.0
EOF
}

if [[ $# -lt 1 || $# -gt 2 ]]; then
  usage
  exit 1
fi

VERSION="${1#v}"
TAG="v${VERSION}"
FRAMEWORK="${2:-net8.0}"
ARTIFACT="${REPO_ROOT}/release/FrameByFrame-v${VERSION}.zip"

if [[ ! "${VERSION}" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$ ]]; then
  echo "Error: Version must resemble 0.1.0 or v0.1.0."
  exit 1
fi

cd "${REPO_ROOT}"

command -v git >/dev/null || { echo "Error: git was not found in PATH."; exit 1; }
command -v gh >/dev/null || { echo "Error: gh was not found in PATH."; exit 1; }
[[ -x "${BUILD_SCRIPT}" ]] || { echo "Error: ${BUILD_SCRIPT} is missing or not executable."; exit 1; }

git rev-parse --is-inside-work-tree >/dev/null 2>&1 || {
  echo "Error: ${REPO_ROOT} is not a Git repository."
  exit 1
}

if [[ -n "$(git status --porcelain)" ]]; then
  echo "Error: The working tree is not clean. Commit or stash changes before releasing."
  git status --short
  exit 1
fi

gh auth status >/dev/null 2>&1 || {
  echo "Error: GitHub CLI is not authenticated. Run 'gh auth login' first."
  exit 1
}

if git rev-parse --verify --quiet "refs/tags/${TAG}" >/dev/null; then
  echo "Error: Tag ${TAG} already exists locally."
  exit 1
fi

if [[ -n "$(git ls-remote --tags origin "refs/tags/${TAG}")" ]]; then
  echo "Error: Tag ${TAG} already exists on origin."
  exit 1
fi

echo "==> Building FrameByFrame ${TAG}"
"${BUILD_SCRIPT}" "${VERSION}" "${FRAMEWORK}"

if [[ ! -f "${ARTIFACT}" ]]; then
  echo "Error: Expected release artifact was not created: ${ARTIFACT}"
  exit 1
fi

echo "==> Creating tag ${TAG}"
git tag -a "${TAG}" -m "FrameByFrame ${TAG}"

echo "==> Pushing tag ${TAG}"
if ! git push origin "${TAG}"; then
  echo "Error: The tag could not be pushed. The local tag ${TAG} was retained."
  exit 1
fi

echo "==> Creating GitHub release ${TAG}"
if ! gh release create "${TAG}" "${ARTIFACT}" \
  --verify-tag \
  --title "FrameByFrame ${TAG}" \
  --generate-notes; then
  echo "Error: The GitHub release could not be created. The pushed tag ${TAG} was retained."
  echo "Retry with: gh release create ${TAG} '${ARTIFACT}' --verify-tag --title 'FrameByFrame ${TAG}' --generate-notes"
  exit 1
fi

echo "Release published successfully: ${TAG}"
gh release view "${TAG}" --json url --jq .url
