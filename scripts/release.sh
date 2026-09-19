#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

# ===== Config =====
# Zip base name:
ZIP_BASENAME="FrameByFrame"
# Override the default project if needed:
#   PROJECT_FILE=FrameByFrame/FrameByFrame.csproj ./scripts/release.sh 1.2.3
PROJECT_FILE="${PROJECT_FILE:-${REPO_ROOT}/FrameByFrame/FrameByFrame.csproj}"

# ===== Usage =====
if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <version> [framework]"
  echo "  version   -> e.g. 1.0.0 (used for /p:Version and zip filename)"
  echo "  framework -> optional, defaults to net8.0"
  exit 1
fi

VERSION="${1#v}"
FRAMEWORK="${2:-net8.0}"

if [[ ! "${VERSION}" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$ ]]; then
  echo "Error: Version must resemble 0.1.0 or v0.1.0."
  exit 1
fi

if [[ ! -f "${PROJECT_FILE}" ]]; then
  echo "Error: Project not found: ${PROJECT_FILE}"
  exit 1
fi
PROJECT_DIR="$(cd -- "$(dirname -- "${PROJECT_FILE}")" && pwd)"
PROJECT_FILE="${PROJECT_DIR}/$(basename -- "${PROJECT_FILE}")"

APP_NAME="$(basename "${PROJECT_FILE%.csproj}")"
echo "Project: ${PROJECT_FILE}"
echo "App    : ${APP_NAME}"
echo "Version: ${VERSION}"
echo "TFM    : ${FRAMEWORK}"

# Ensure tools exist
command -v dotnet >/dev/null || { echo "dotnet not found in PATH"; exit 1; }
command -v zip >/dev/null || { echo "zip not found. Install 'zip' and retry."; exit 1; }

# RIDs to publish
PUBLISH_ROOT="${REPO_ROOT}/release/publish"
RIDS=( "linux-x64" "win-x64" "osx-arm64" "osx-x64")

# Clean old publish outputs
dotnet clean "${PROJECT_FILE}" -c Release

# Keep native libraries beside the executable: MonoGame loads SDL/OpenAL
# dynamically and cannot find them in the single-file extraction directory.
# Build & publish
for RID in "${RIDS[@]}"; do
  echo "==> Publishing for ${RID}"
  PUBLISH_DIR="${PUBLISH_ROOT}/${RID}"
  rm -rf "${PUBLISH_DIR}"
  dotnet publish "${PROJECT_FILE}" -c Release -r "${RID}" --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=false \
    -p:UseAppHost=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:Version="${VERSION}" \
    -p:ContinuousIntegrationBuild=true \
    -f "${FRAMEWORK}" -o "${PUBLISH_DIR}"
done

# Stage files for zipping
STAGE_ROOT="${REPO_ROOT}/release"
STAGE_DIR="${STAGE_ROOT}/${ZIP_BASENAME}-v${VERSION}"
rm -rf "${STAGE_DIR}"
mkdir -p "${STAGE_DIR}"

copy_build () {
  local rid="$1"
  local dest_name="$2"   # folder name inside the zip
  local publish_dir="${PUBLISH_ROOT}/${rid}"
  local required

  if [[ ! -d "${publish_dir}" ]]; then
    echo "Error: Missing publish dir ${publish_dir}"
    exit 1
  fi

  if [[ "${rid}" == "win-x64" ]]; then
    for required in "${APP_NAME}.exe" SDL2.dll soft_oal.dll Content/UIFont.xnb; do
      if [[ ! -f "${publish_dir}/${required}" ]]; then
        echo "Error: Windows publish is missing ${required}"
        exit 1
      fi
    done
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
- Extract the entire archive before running. Keep each executable with its native libraries and Content folder.
- If double-clicking doesn't work on Linux/macOS, try running from a terminal: chmod +x and then ./${APP_NAME}
EOF

# Create the zip in release/
OUT_ZIP="${STAGE_ROOT}/${ZIP_BASENAME}-v${VERSION}.zip"
echo "==> Creating ${OUT_ZIP}"
rm -f "${OUT_ZIP}"
( cd "${STAGE_ROOT}" && zip -r "${OUT_ZIP}" "$(basename "${STAGE_DIR}")" >/dev/null )

echo ""
echo "Done!"
echo "Artifact: ${OUT_ZIP}"
echo "Staged at: ${STAGE_DIR}"
