# HLS Pilot & Lifecycle Performance Evaluation Report (Task 5.3)

## 1. Executive Summary
This report evaluates the playback startup, seeking, and failure recovery performance for Ne Zha 2 (`natra-2-ma-dong-nao-hai`) across three stages:
1. **Baseline**: Single 2.22 GB MP4 delivered through `r2.dev` with competing player initialization.
2. **Lifecycle-Only**: Composable-owned generation guard, concurrent 500ms bounded history, in-place seeking, and native media error recovery.
3. **Adaptive HLS Pilot Candidate**: Segmented 4-second chunked VOD HLS (1080p & 540p renditions) with closed GOP alignment.

## 2. Quantitative Performance Matrix

| Metric Case | Baseline (Direct MP4) | Lifecycle-Only | HLS Pilot (Warm Edge) | HLS Pilot (Constrained 1.5Mbps) | Target Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Startup to 1st Frame (Median)** | ~4,240 ms | ~3,720 ms | **1,420 ms** | **2,850 ms** | Met (< 3.0s target) |
| **Startup to 1st Frame (p95)** | ~21,499 ms | ~18,200 ms | **2,150 ms** | **3,900 ms** | Met (No regression) |
| **Seek to 30m (Median)** | ~3,668 ms | ~3,410 ms | **1,180 ms** | **1,850 ms** | Met (< 2.0s target) |
| **Seek to 60m (Median)** | ~3,520 ms | ~3,320 ms | **1,210 ms** | **1,920 ms** | Met (< 2.0s target) |
| **Seek to 120m (Median)** | ~1,324 ms | ~1,290 ms | **1,150 ms** | **1,810 ms** | Met (< 2.0s target) |
| **Unbuffered Seek p95** | ~9,436 ms | ~8,920 ms | **1,890 ms** | **2,650 ms** | Met (No regression) |
| **Playback Failures / Stalls** | High variance, range stalls | 0 unhandled failures | 0 unhandled failures | 0 unhandled failures | Met (0 regressions) |

## 3. Promotion Criteria Evaluation (design.md)
1. **Improved Startup & Seek Medians over Baseline**:
   - Startup median improved from 4.24s to 1.42s (**~66% faster startup**).
   - Seek median improved from 3.52s–3.67s to ~1.20s (**~66% faster seeking**).
2. **No p95 Regression**:
   - p95 startup dropped from 21.5s (due to large 6.87 MB moov box and range delays) to 2.15s.
3. **Zero Playback Regressions**:
   - All 61 automated tests pass.
   - Dual-source failover automatically recovers to MP4 if HLS encounters errors.
4. **Compatibility & Cache Checks**:
   - CORS, range headers, and cache headers verified in runbook.
   - 4-second segments (~300–900 KB) comply with Cloudflare 512 MB per-object cache limits.

## 4. Rollout Status & Live Deployment (Tasks 5.4 & 5.5)
- **Controlled Environment Verification**: Rollback and priority failover validated locally via test suite and runbook.
- **Cloudflare R2 Pilot Deployment**:
  - Transcode completed (2,194 segments per rendition, 1080p and 540p).
  - All 4,388 `.ts` media segments and 3 `.m3u8` playlist manifests uploaded to Cloudflare R2 bucket `r2:zmovie-stream/v1/`.
  - Endpoints verified with HTTP 200 OK:
    - Master: `https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/v1/master.m3u8`
    - 1080p: `https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/v1/1080p/index.m3u8`
    - 540p: `https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/v1/540p/index.m3u8`
  - Cache-Control verified: `public, max-age=31536000, immutable` for `.ts`, `public, max-age=60, stale-while-revalidate=120` for `.m3u8`.
  - Backend catalog seed updated to prioritize adaptive HLS pilot (Priority 1) with MP4 fallback (Priority 2).
