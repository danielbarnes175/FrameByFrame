#!/usr/bin/env bash
set -euo pipefail

# ===== Config =====
# Zip base name:
ZIP_BASENAME="FrameByFrame"
# If your .csproj isn't in this folder, set PROJECT_FILE to its path before running:
#   PROJECT_FILE=FrameByFrame/FrameByFrame.csproj ./scripts/release.sh 1.2.3
PROJECT_FILE="${PROJECT_FILE:-}"

# ===== Usage =====
if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <version> [framework]"
  echo "  version   -> e.g. 1.0.0 (used for /p:Version and zip filename)"
  echo "  framework -> optional, defaults to net8.0"
  exit 1
fi

VERSION="$1"
FRAMEWORK="${2:-net8.0}"

# Find the csproj if not specified
if [[ -z "$PROJECT_FILE" ]]; then
  # pick first .csproj in current dir
  PROJECT_FILE="$(ls -1 *.csproj 2>/dev/null | head -n1 || true)"
fi
if [[ -z "${PROJECT_FILE}" || ! -f "${PROJECT_FILE}" ]]; then
  echo "Error: Could not find a .csproj here. Set PROJECT_FILE env var to your project file."
  exit 1
fi

APP_NAME="$(basename "${PROJECT_FILE%.csproj}")"
echo "Project: ${PROJECT_FILE}"
echo "App    : ${APP_NAME}"
echo "Version: ${VERSION}"
echo "TFM    : ${FRAMEWORK}"

# Ensure tools exist
command -v dotnet >/dev/null || { echo "dotnet not found in PATH"; exit 1; }
command -v zip >/dev/null || { echo "zip not found. Install 'zip' and retry."; exit 1; }

# RIDs to publish
RIDS=( "linux-x64" "win-x64" "osx-arm64" "osx-x64")

# Clean old publish outputs
dotnet clean "${PROJECT_FILE}" -c Release

# Build & publish
for RID in "${RIDS[@]}"; do
  echo "==> Publishing for ${RID}"
  dotnet publish "${PROJECT_FILE}" -c Release -r "${RID}" --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:UseAppHost=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:Version="${VERSION}" \
    -p:ContinuousIntegrationBuild=true \
    -f "${FRAMEWORK}"
done

# Stage files for zipping
STAGE_ROOT="release"
STAGE_DIR="${STAGE_ROOT}/${ZIP_BASENAME}-v${VERSION}"
rm -rf "${STAGE_DIR}"
mkdir -p "${STAGE_DIR}"

copy_build () {
  local rid="$1"
  local dest_name="$2"   # folder name inside the zip
  local publish_dir="FrameByFrame/bin/Release/${FRAMEWORK}/${rid}/publish"

  if [[ ! -d "${publish_dir}" ]]; then
    echo "Error: Missing publish dir ${publish_dir}"
    exit 1
  fi

  echo "==> Staging ${rid} -> ${dest_name}"
  mkdir -p "${STAGE_DIR}/${dest_name}"
  cp -a "${publish_dir}/." "${STAGE_DIR}/${dest_name}/"

  # Ensure executables are marked as such (Linux/macOS)
  if [[ "${rid}" == "linux-x64" ]]; then
    chmod +x "${STAGE_DIR}/${dest_name}/${APP_NAME}" || true
  elif [[ "${rid}" == "osx-arm64" ]]; then
    chmod +x "${STAGE_DIR}/${dest_name}/${APP_NAME}" || true
  elif [[ "${rid}" == "osx-x64" ]]; then
    chmod +x "${STAGE_DIR}/${dest_name}/${APP_NAME}" || true
  fi
}

copy_build "linux-x64"   "linux-x64"
copy_build "win-x64"     "windows-x64"
copy_build "osx-arm64"   "macos-arm64"
copy_build "osx-x64"     "macos-x64"

# Drop a README with simple run instructions
cat > "${STAGE_DIR}/README.txt" <<EOF
${ZIP_BASENAME} v${VERSION}

Contents:
- linux-x64/      -> Run ./${APP_NAME}
- windows-x64/    -> Run ${APP_NAME}.exe
- macos-arm64/    -> Run ./${APP_NAME}
- macos-x64/      -> Run ./${APP_NAME}

Notes:
- If double-clicking doesn't work on Linux/macOS, try running from a terminal: chmod +x and then ./${APP_NAME}
EOF

# Create the zip beside release/
OUT_ZIP="release/${ZIP_BASENAME}-v${VERSION}.zip"
echo "==> Creating ${OUT_ZIP}"
( cd "${STAGE_ROOT}" && zip -r "../${OUT_ZIP}" "$(basename "${STAGE_DIR}")" >/dev/null )

echo ""
echo "Done!"
echo "Artifact: ${OUT_ZIP}"
echo "Staged at: ${STAGE_DIR}"
