# Natra Adaptive HLS Pilot Runbook & Rollout Configuration (Phase 4)

## 4.1 Offline Preparation Tooling & Encoder Specification
The repeatable script `scripts/prepare-hls-pilot.sh` prepares the adaptive HLS pilot from the inspected Natra MP4 source (`https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/phim.mp4`).

### Renditions Matrix (Non-upscaled)
1. **1080p Rendition (Native Aspect Ratio 12:5)**:
   - Video Resolution: `1920x800`
   - Video Bitrate: `1800 kbps` (Maxrate: `2000 kbps`, Bufsize: `3000 kbps`)
   - Profile/Level: H.264 Main Profile, Level 4.0
   - Audio: AAC-LC, Stereo, 48,000 Hz, 128 kbps
2. **540p Rendition (Mobile / Constrained Profile)**:
   - Video Resolution: `960x400`
   - Video Bitrate: `600 kbps` (Maxrate: `750 kbps`, Bufsize: `1200 kbps`)
   - Profile/Level: H.264 Main Profile, Level 3.1
   - Audio: AAC-LC, Stereo, 48,000 Hz, 96 kbps

### GOP & Segment Boundary Alignment
- Frame Rate: Constant 24.000 fps (`-r 24`)
- Closed GOP Length: 96 frames (`-g 96 -keyint_min 96 -sc_threshold 0`)
- Segment Target Duration: exactly 4.0 seconds per segment (`-hls_time 4`)
- Segmentation Flags: `-hls_flags independent_segments -hls_playlist_type vod`
- Segment Size: ~900 KB per 1080p segment, ~300 KB per 540p segment (all well within Cloudflare 512 MB per-object cache limits)

## 4.2 Manifest Hierarchy & Versioned Output
- Target prefix: `v1/`
- Structure:
  ```
  dist/hls-pilot/v1/
  ├── master.m3u8
  ├── 1080p/
  │   ├── index.m3u8
  │   ├── seg_0000.ts
  │   └── ...
  └── 540p/
      ├── index.m3u8
      ├── seg_0000.ts
      └── ...
  ```
- Validation tool: `scripts/validate-hls-pilot.sh`.

## 4.3 R2 Custom Domain, Cache Policy & Upload Ordering
- **Upload Order**: Upload media segments (`.ts`) before publishing playlist manifests (`.m3u8`) to avoid 404s during initial playback.
- **HTTP Cache Headers**:
  - Versioned segments (`*.ts`): `Cache-Control: public, max-age=31536000, immutable`
  - Manifests (`*.m3u8`): `Cache-Control: public, max-age=60, stale-while-revalidate=120`
- **CORS Configuration**:
  ```json
  [
    {
      "AllowedOrigins": ["*"],
      "AllowedMethods": ["GET", "HEAD"],
      "AllowedHeaders": ["Range", "Origin", "Accept"],
      "ExposeHeaders": ["Content-Length", "Content-Range", "ETag"],
      "MaxAgeSeconds": 86400
    }
  ]
  ```

## 4.4 Controlled Environment Candidate & Rollback Procedure
In the controlled test environment, Natra's playback configuration is updated to prioritize the adaptive HLS pilot while retaining the original MP4 as fallback.

### Candidate Source Priorities (Natra Pilot)
```json
[
  {
    "provider": "Cloudflare R2 Pilot (Adaptive HLS)",
    "url": "https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/v1/master.m3u8",
    "format": "hls",
    "priority": 1
  },
  {
    "provider": "Cloudflare R2 Direct Video (MP4 Fallback)",
    "url": "https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/phim.mp4",
    "format": "video",
    "priority": 2
  }
]
```

### Rollback Procedure
If the pilot demonstrates any playback regression, restore the baseline configuration immediately from `baseline-configuration.json`:
1. Demote or disable the HLS candidate.
2. Set `https://pub-a6d16eb1790945d6a27f6e7a28c2660b.r2.dev/phim.mp4` back to Priority 1.
3. Verify playback directly in browser.
