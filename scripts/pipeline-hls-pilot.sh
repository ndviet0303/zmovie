#!/usr/bin/env bash
set -euo pipefail

echo "========================================================"
echo "Starting Natra HLS Pilot Transcode & R2 Upload Pipeline"
echo "Time: $(date)"
echo "========================================================"

echo ""
echo "[STEP 1/3] Preparing HLS pilot with dual-rendition single-pass..."
./scripts/prepare-hls-pilot.sh /tmp/phim.mp4 v1 ./dist/hls-pilot/v1 veryfast

echo ""
echo "[STEP 2/3] Validating generated HLS pilot..."
./scripts/validate-hls-pilot.sh ./dist/hls-pilot/v1

echo ""
echo "[STEP 3/3] Uploading HLS pilot to Cloudflare R2..."
./scripts/upload-hls-pilot.sh ./dist/hls-pilot/v1

echo ""
echo "========================================================"
echo "HLS Pilot Transcode & R2 Upload Pipeline COMPLETED!"
echo "Finished at: $(date)"
echo "========================================================"
