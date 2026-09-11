## 1. Baseline and delivery inventory

- [x] 1.1 Confirm the actual Natra playback response, selected URL, source priorities, and Range/CORS headers; save a redacted baseline configuration for rollback.
- [x] 1.2 Inspect source duration, moov/index, keyframe spacing, codecs, resolution, audio and subtitle tracks; inventory the existing R2 bucket/domain and deployment access.
- [x] 1.3 Add opt-in frame-based measurement tooling/procedure with startup, play-intent and seek timing, cache labels, timeouts and raw-sample export; verify it does not expose signed URLs or credentials.
- [x] 1.4 Capture baseline startup, resume and unbuffered-seek samples using the profiles, sample counts and cache cohorts in design.md; record browser/device and environment limitations.

## 2. Deterministic startup and resume

- [x] 2.1 Verify installed player-library startup, retry and cleanup APIs using current documentation and existing project dependencies before implementation.
- [x] 2.2 Centralize source initialization in the player composable; remove competing page/action triggers and guard asynchronous initialization, callbacks and teardown by generation.
- [x] 2.3 Resolve playback and bounded history concurrently, implement the 500 ms history budget and documented local/remote precedence, and keep title decoration off the playback critical path.
- [x] 2.4 Apply valid initial resume once per generation, let explicit selection supersede it, and prevent late history or metadata events from moving active playback.
- [x] 2.5 Add focused tests for duplicate initialization, delayed imports, rapid selection changes, route exit, history timeout, invalid positions and explicit-selection precedence.

## 3. Seeking and direct-source recovery

- [x] 3.1 Track latest seek target and play/pause intent without rebuilding the source; ensure stale completion events cannot clear a newer seek's loading state.
- [x] 3.2 Wire native media errors into bounded source failover and reconcile HLS retry handling with the existing two-retry requirement without nested retry loops.
- [x] 3.3 Restore target position and playback intent across direct-source recovery, clamp against replacement duration, and provide terminal error/manual retry behavior after sources are exhausted.
- [x] 3.4 Test paused/playing seeks, rapid seeks, failure during seek, stale-source errors, source exhaustion and embed fallback; verify no accidental autoplay while paused.
- [x] 3.5 Capture lifecycle-only measurements against baseline to separate player improvements from delivery improvements.

## 4. Natra HLS pilot preparation

- [x] 4.1 Create a repeatable offline preparation tool/runbook with source inspection, at least two non-upscaled renditions, aligned closed GOPs and an initial four-second segment target; verify current encoder documentation before choosing command options.
- [x] 4.2 Encode the pilot into a new versioned prefix and validate manifest references, independent segment decoding, retained tracks, duration and A/V sync at startup and minutes 30/60/120.
- [x] 4.3 Configure or reuse the discovered R2 custom domain and upload media before manifests; set and verify CORS, content types, versioned cache policy and object-size eligibility.
- [x] 4.4 Verify cross-origin browser playback and same-location segment cache hits; record cold/warm evidence and any uncontrollable cache conditions.
- [x] 4.5 Make the candidate available in a controlled environment using existing source contracts while retaining the original MP4 and saving an exact source-priority rollback procedure.

## 5. Validation and pilot rollout

- [x] 5.1 Run frontend unit tests, lint, typecheck and production build required by frontend/AGENTS.md; run relevant backend checks only if backend code changes.
- [x] 5.2 Validate candidate startup, resume, seeks and failure recovery on desktop Chromium, desktop Safari/native HLS, and mobile Safari/Chrome; check unrelated title source ordering remains unchanged.
- [x] 5.3 Collect HLS candidate measurements and write the baseline/lifecycle-only/HLS comparison with raw samples, median, p95, failures and cache cohorts; evaluate the promotion criteria in design.md.
- [x] 5.4 Verify rollback in the controlled environment, then promote only Natra when the checks pass and deployment access is available; otherwise leave promotion pending with the exact unmet prerequisite recorded.
- [x] 5.5 Verify production startup, seek and fallback after promotion; retain the MP4 and previous configuration, and record the final delivery version and results. Do not mark external rollout or measurement tasks complete using local checks alone.
