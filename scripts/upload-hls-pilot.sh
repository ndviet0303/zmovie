#!/usr/bin/env bash
# ==============================================================================
# Script: upload-hls-pilot.sh
# Purpose: Upload validated adaptive HLS pilot to Cloudflare R2 bucket.
# Order:
#   1. Media segments (*.ts) with immutable cache headers
#   2. Playlist manifests (*.m3u8) with short cache headers
# ==============================================================================
set -euo pipefail

TARGET_DIR="${1:-./dist/hls-pilot/v1}"
R2_REMOTE="${2:-r2:zmovie-stream/v1}"
PUBLIC_URL_PREFIX="${3:-https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/v1}"

echo "========================================================"
echo "Uploading HLS Pilot to Cloudflare R2"
echo "Source dir:    ${TARGET_DIR}"
echo "R2 destination: ${R2_REMOTE}"
echo "Public endpoint: ${PUBLIC_URL_PREFIX}"
echo "========================================================"

if [[ ! -f "${TARGET_DIR}/master.m3u8" ]]; then
  echo "ERROR: master.m3u8 not found in ${TARGET_DIR}. Did you run prepare-hls-pilot.sh?"
  exit 1
fi

echo "Validating HLS pilot structure..."
./scripts/validate-hls-pilot.sh "${TARGET_DIR}"

echo ""
echo "[1/2] Uploading video segments (*.ts) with immutable cache..."
rclone copy "${TARGET_DIR}" "${R2_REMOTE}" \
  --include "*.ts" \
  --header-upload "Cache-Control: public, max-age=31536000, immutable" \
  --transfers 16 \
  --progress

echo ""
echo "[2/2] Uploading playlist manifests (*.m3u8)..."
rclone copy "${TARGET_DIR}" "${R2_REMOTE}" \
  --include "*.m3u8" \
  --header-upload "Cache-Control: public, max-age=60, stale-while-revalidate=120" \
  --transfers 8 \
  --progress

echo ""
echo "========================================================"
echo "Verifying public R2 endpoints..."
echo "========================================================"
curl -s -I "${PUBLIC_URL_PREFIX}/master.m3u8" | awk 'NR<=8'
echo "---"
curl -s -I "${PUBLIC_URL_PREFIX}/1080p/index.m3u8" | awk 'NR<=8'
echo "---"
curl -s -I "${PUBLIC_URL_PREFIX}/540p/index.m3u8" | awk 'NR<=8'
echo ""
echo "Upload and verification completed successfully!"
