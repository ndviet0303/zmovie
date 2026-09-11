#!/usr/bin/env bash
# ==============================================================================
# Script: prepare-hls-pilot.sh
# Purpose: Offline, repeatable preparation of adaptive HLS pilot for Natra.
# Requirements:
#   - At least two non-upscaled renditions (1920x800 and 960x400)
#   - Strictly aligned closed GOPs (-g 96 -keyint_min 96 -sc_threshold 0 at 24 fps)
#   - 4-second segment target with independent segment decoding
#   - Versioned output directory structure and master playlist
# ==============================================================================
set -euo pipefail

SOURCE_URL="${1:-https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/phim.mp4}"
VERSION="${2:-v1}"
OUTPUT_DIR="${3:-./dist/hls-pilot/${VERSION}}"
PRESET="${4:-veryfast}"

# Check for local cached file
if [[ -f "/tmp/phim.mp4" && "${SOURCE_URL}" =~ ^https?:// ]]; then
  echo "Found local cached source at /tmp/phim.mp4, using it for high-speed encoding."
  SOURCE_URL="/tmp/phim.mp4"
fi

echo "========================================================"
echo "Preparing Natra HLS Pilot (${VERSION})"
echo "Source:  ${SOURCE_URL}"
echo "Output:  ${OUTPUT_DIR}"
echo "Preset:  ${PRESET}"
echo "========================================================"

mkdir -p "${OUTPUT_DIR}/1080p"
mkdir -p "${OUTPUT_DIR}/540p"

echo "[1/3] Inspecting source stream..."
ffprobe -v error -show_entries format=duration,size,bit_rate -show_entries stream=codec_name,width,height,r_frame_rate,channels,sample_rate "${SOURCE_URL}"

echo "[2/3] Generating 1080p and 540p renditions in single pass..."
ffmpeg -y -i "${SOURCE_URL}" \
  -filter_complex "[0:v]split=2[v1][v2];[v1]scale=1920:800[v1out];[v2]scale=960:400[v2out]" \
  -map "[v1out]" -c:v:0 libx264 -preset "${PRESET}" -profile:v:0 main -level:v:0 4.0 \
  -b:v:0 1800k -maxrate:v:0 2000k -bufsize:v:0 3000k \
  -r 24 -g 96 -keyint_min 96 -sc_threshold 0 \
  -map 0:a -c:a:0 aac -b:a:0 128k -ar 48000 -ac 2 \
  -f hls \
  -hls_time 4 \
  -hls_playlist_type vod \
  -hls_flags independent_segments \
  -hls_segment_filename "${OUTPUT_DIR}/1080p/seg_%04d.ts" \
  "${OUTPUT_DIR}/1080p/index.m3u8" \
  -map "[v2out]" -c:v:1 libx264 -preset "${PRESET}" -profile:v:1 main -level:v:1 3.1 \
  -b:v:1 600k -maxrate:v:1 750k -bufsize:v:1 1200k \
  -r 24 -g 96 -keyint_min 96 -sc_threshold 0 \
  -map 0:a -c:a:1 aac -b:a:1 96k -ar 48000 -ac 2 \
  -f hls \
  -hls_time 4 \
  -hls_playlist_type vod \
  -hls_flags independent_segments \
  -hls_segment_filename "${OUTPUT_DIR}/540p/seg_%04d.ts" \
  "${OUTPUT_DIR}/540p/index.m3u8"

echo "[3/3] Writing master playlist..."
cat << 'EOF' > "${OUTPUT_DIR}/master.m3u8"
#EXTM3U
#EXT-X-VERSION:3

#EXT-X-STREAM-INF:BANDWIDTH=2100000,AVERAGE-BANDWIDTH=1928000,RESOLUTION=1920x800,FRAME-RATE=24.000,CODECS="avc1.4d4028,mp4a.40.2"
1080p/index.m3u8

#EXT-X-STREAM-INF:BANDWIDTH=750000,AVERAGE-BANDWIDTH=696000,RESOLUTION=960x400,FRAME-RATE=24.000,CODECS="avc1.4d401f,mp4a.40.2"
540p/index.m3u8
EOF

echo "========================================================"
echo "HLS pilot generation completed at: ${OUTPUT_DIR}"
echo "Master manifest: ${OUTPUT_DIR}/master.m3u8"
echo "========================================================"
