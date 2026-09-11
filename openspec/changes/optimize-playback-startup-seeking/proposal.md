## Why

Viewers experience long waits before a movie starts and after seeking. The current player can initialize the same source multiple times, while the Natra MP4 delivery path shows limited throughput; improving both playback orchestration and media delivery is necessary to address these waits.

## What Changes

- Establish one cancellable playback initialization per selected episode/source and ignore stale asynchronous work.
- Resolve the initial episode and saved position before loading its media, with a bounded wait for remote history and no late interruption of playback.
- Preserve playback position and play/pause intent across direct-source recovery; handle native MP4 errors as well as HLS errors.
- Prepare a Natra-only HLS pilot with short, independently decodable segments and adaptive renditions delivered through an R2 custom domain with verified cache behavior. Retain the existing MP4 as fallback and rollback source.
- Measure actual first-frame and post-seek frame latency using a reproducible baseline and before/after browser runs, including cold and warm delivery paths.

## Capabilities

### New Capabilities
- `playback/startup-and-seeking`: Deterministic startup, resume, seeking, and frame-based latency verification.
- `playback/pilot-media-delivery`: Reversible, cacheable adaptive HLS delivery for the Natra pilot.

### Modified Capabilities
- `playback/player-enhancements`: Extend direct-source failover to native video, preserve position and playback intent, and prevent stale source initialization from taking over.

## Impact

- Frontend watch-page orchestration, `useWatchPlayer`, `PlayerViewport`, and focused player tests; existing catalog transport and source contracts remain compatible.
- Natra catalog source priorities, offline media preparation tooling/runbook, R2 assets, custom-domain delivery, CORS and cache configuration.
- Browser performance evidence and a rollback procedure are deliverables; production rollout depends on verified pilot results and available deployment access.
- Reuse the installed HLS player. No new streaming proxy, full-catalog migration, transcoding farm, watch-party redesign, or unrelated player feature work.
