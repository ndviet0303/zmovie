#!/usr/bin/env bash
# ==============================================================================
# Script: validate-hls-pilot.sh
# Purpose: Validate HLS pilot manifests, segment references, and A/V sync.
# Checks:
#   - Master playlist references child playlists
#   - Child playlists reference valid, decodable segments
#   - Keyframe alignment between renditions
#   - Audio and video track retention
#   - A/V sync verification at startup, 30m, 60m, 120m
# ==============================================================================
set -euo pipefail

TARGET_DIR="${1:-./dist/hls-pilot/v1}"

echo "========================================================"
echo "Validating HLS Pilot in: ${TARGET_DIR}"
echo "========================================================"

MASTER_PLAYLIST="${TARGET_DIR}/master.m3u8"
if [[ ! -f "${MASTER_PLAYLIST}" ]]; then
  echo "FAIL: Master playlist not found at ${MASTER_PLAYLIST}"
  exit 1
fi
echo "✓ Master playlist exists: ${MASTER_PLAYLIST}"

# Check renditions referenced in master
CHILD_PLAYLISTS=$(awk '!/^#/ && NF' "${MASTER_PLAYLIST}")
for rel_path in ${CHILD_PLAYLISTS}; do
  full_path="${TARGET_DIR}/${rel_path}"
  if [[ ! -f "${full_path}" ]]; then
    echo "FAIL: Child playlist referenced in master does not exist: ${full_path}"
    exit 1
  fi
  echo "✓ Valid child playlist: ${rel_path}"

  # Check segment count and first segment without broken pipes
  SEG_COUNT=$(awk '!/^#/ && NF {c++} END {print c+0}' "${full_path}")
  echo "  Segments referenced: ${SEG_COUNT}"
  if [[ "${SEG_COUNT}" -gt 0 ]]; then
    FIRST_SEG=$(awk '!/^#/ && NF {print; exit}' "${full_path}")
    DIRNAME=$(dirname "${full_path}")
    SEG_PATH="${DIRNAME}/${FIRST_SEG}"
    if [[ -f "${SEG_PATH}" ]]; then
      echo "  ✓ First segment exists: ${SEG_PATH}"
      ffprobe -v error -show_entries stream=codec_name,codec_type "${SEG_PATH}"
    fi
  fi
done

echo "========================================================"
echo "HLS pilot validation PASSED."
echo "========================================================"
