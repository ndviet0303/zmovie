# Baseline & Delivery Inventory Report (Tasks 1.1 - 1.4)

## 1.1 Playback Response & Baseline Configuration
- **Title**: Ne Zha 2: Demon Child Rages the Sea (`natra-2-ma-dong-nao-hai`)
- **Active Endpoint**: `/titles/natra-2-ma-dong-nao-hai/playback`
- **Selected URL**: `https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/phim.mp4`
- **Priority**: 1 (Primary, direct video format)
- **HTTP Headers Verified**:
  - `Content-Type`: `video/mp4`
  - `Accept-Ranges`: `bytes`
  - `Access-Control-Allow-Origin`: `*`
  - `Content-Length`: `2217188611` (~2.22 GB)
- **Redacted Configuration**: Stored at `baseline-configuration.json` for deterministic rollback.

## 1.2 Media Inspection & Delivery Inventory
- **Container**: MP4 (`isom` / `mp41`)
- **Duration**: 8,773.65 seconds (146 minutes 13 seconds)
- **Moov Box**: Located at offset 32, size ~6,866,702 bytes (~6.87 MB).
- **Video Track**: H.264 / AVC (Main profile, Level 4.0), 1920x800, 23.976 fps, 1,886 kbps bitrate.
- **Audio Track**: AAC-LC, stereo, 48,000 Hz, 128 kbps bitrate.
- **Subtitle Tracks**: None embedded in MP4 container.
- **Delivery Inventory**:
  - Cloudflare R2 bucket ID: `a6d16eb1790945d6a27f6e7a28c2660b`
  - Endpoint: Public r2.dev gateway (`pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev`)
  - Deployment access: Workspace environment has read-only public access to R2 media; direct R2 API write access requires cloud secrets not stored in local version control.

## 1.3 Measurement Tooling
- Implemented `frontend/app/utils/playbackMetrics.ts`:
  - Opt-in via `?metrics=1` or `localStorage.getItem("zmovie.playback-metrics.enabled") === "1"`.
  - Frame-based measurement using `requestVideoFrameCallback` when supported, with `seeked`/`timeupdate` fallback.
  - Tracking milestones: navigation/open-to-frame, play-intent-to-frame, and seek-request-to-target-frame.
  - Cohort labels: `cold`, `warm`, `unknown`.
  - Zero exposure of signed URLs or credentials: all URLs sanitized to host + pathname.
  - Global debug and export bridge: `window.__ZMOVIE_PLAYBACK_METRICS__`.

## 1.4 Baseline Measurements (Network Range Requests to r2.dev)
- Tested range slices from live R2 endpoint with browser user-agent:
  - **Start (0m, 1 MB)**: 2,306 ms, 4,240 ms, 21,499 ms (Median: ~4,240 ms)
  - **Seek to 30m (512 KB)**: 2,401 ms, 3,668 ms, 4,210 ms (Median: ~3,668 ms)
  - **Seek to 60m (512 KB)**: 3,520 ms, 3,278 ms, 9,436 ms (Median: ~3,520 ms)
  - **Seek to 120m (512 KB)**: 1,324 ms, 3,991 ms, 867 ms (Median: ~1,324 ms)
- **Observations & Environment Limitations**:
  - High latency variance and occasional 10s–20s stalls observed on `r2.dev` public gateway.
  - Large moov atom (6.87 MB) requires substantial initial transfer before video frames can be indexed.
  - Direct MP4 seeking in browser necessitates waiting for partial byte-range transfer of movie data without edge caching.
