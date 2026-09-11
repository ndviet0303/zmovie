## Context

See proposal.md for motivation. The watch page invokes initialization after loading data, while the composable also watches source/video changes; episode/source actions invoke initialization again. HLS initialization awaits a dynamic import without a generation guard. Native video errors are not wired into recovery. History is fetched asynchronously after playback data is published, potentially changing episodes after loading has begun; local resume is applied from more than one media event.

Exploratory evidence on 2026-09-10 concerns the seed URL, not a verified production catalog response: the Natra MP4 is 2,217,188,611 bytes, serves valid byte ranges and CORS, and has a 6,866,702-byte moov box at offset 32. One 1 MiB sample took 5.48 seconds (~1.53 Mbps). These observations suggest delivery and index overhead warrant measurement; they do not establish the screenshot's root cause. The existing MP4 already has front-loaded metadata.

The current seekbar commits on click, so pointer-move debouncing is not a solution to the reported wait. The catalog already represents prioritized HLS, direct video, and embed sources. No response-contract migration is needed.

## Goals / Non-Goals

**Goals:** Separate application delay, initial media acquisition, and decode/display latency; make source ownership deterministic; validate a reversible Natra delivery experiment.

**Non-Goals:** Build a transcoding service, proxy video through the application backend, migrate other titles, redesign player controls, change autoplay policy, or optimize unrelated history writes and social features.

## Decisions

### 1. One owner for playback initialization

Use a single composable-owned transition driven by a stable episode/source identity and the active video element. Pages and selection actions update intent rather than call initialization. Assign each transition a generation token; invalidate it before teardown, on selection changes, and on unmount. Check ownership after every asynchronous boundary and inside media/HLS callbacks. Tear down listeners, pending timers, and the previous HLS instance. Ignore duplicate transitions for an unchanged identity.

This addresses both duplicate resets and stale async completion. Merely removing the page's call would leave source actions and delayed imports vulnerable. Keep transport in services and orchestration in composables under the existing frontend architecture.

### 2. Resolve resume with a bounded startup phase

Fetch catalog playback and history concurrently; title decoration need not gate playable data. Default the remote-history decision budget to 500 ms from starting that request. Use timely remote history to select the episode; for that episode prefer a valid local position, then a valid remote position. On timeout/failure use the default episode and its local position. Preserve current local validation (older than 90 days, <=5 seconds, or within 30 seconds of the end is not resumed). Check actual media duration before applying a position.

Explicit episode selection wins immediately. Freeze the automatic resume decision once media initialization starts; late history may populate unrelated state but cannot seek or change episode. Apply the selected resume once per generation. For HLS, provide the intended start position before content loading using APIs verified against the installed version during implementation. For MP4/native HLS, apply it as soon as metadata permits; do not promise zero browser metadata requests.

Waiting indefinitely for history worsens startup, while starting immediately and applying late history creates wasted loads. The bounded phase trades at most 500 ms of remote lookup time for a stable initial selection.

### 3. Seek and recovery share explicit playback intent

Store the latest target and play/pause intent separately from observed time updates. A seek never rebuilds the source. New seeks supersede prior targets; loading completion must match the active generation and latest target. Paused seeking should reveal a frame without calling play. Preserve the target across direct-source failures and clamp it against the replacement duration.

Wire native video errors into bounded failover. Preserve the existing HLS requirement of two retry attempts for network/manifest failures, avoiding a second retry loop on top of library retries. Terminal native media errors advance once; do not wrap around the source list. A manual retry starts a new attempt. Embed fallback retains existing behavior without promising cross-origin position control. Verify current player-library error and retry APIs before implementation.

### 4. Prepare an offline HLS pilot, retaining MP4

Inspect source codec, resolution, frame rate, audio, subtitles and timeline before encoding. Produce versioned VOD HLS with an initial 4-second segment target, closed GOP/keyframes aligned across renditions, and at least two bitrate levels without upscaling. Determine concrete bitrate/resolution settings from source inspection; include a lower rendition suitable for the constrained-network test. Preserve required tracks and verify A/V sync at the beginning and each seek target. Reuse the installed HLS engine and native HLS fallback.

Package and upload through an offline, repeatable tool/runbook; publish manifests only after all referenced media is present. Use a separate versioned prefix and custom domain, correct content types/CORS, immutable caching for versioned assets, and explicit manifest cache behavior. Verify cold fetches and repeated same-location segment cache hits. Existing MP4 stays intact as lower priority.

Alternatives: moving moov is unnecessary for the inspected file; custom-domain MP4 alone does not solve its oversized cache object; segmenting a single high-bitrate copy does not address constrained bandwidth. A managed video service or full transcoding farm expands the scope and operational model.

Cloudflare documentation consulted during exploration: [R2 public endpoint limits](https://developers.cloudflare.com/r2/platform/limits/), [R2 custom domains](https://developers.cloudflare.com/r2/buckets/public-buckets/), and [cacheable size limits](https://developers.cloudflare.com/cache/concepts/default-cache-behavior/#cacheable-size-limits). The documented Free/Pro/Business cache limit is 512 MB per object; verify the actual account configuration during delivery setup. Splitting into small objects avoids relying on cache support for the 2.22 GB original.

### 5. Use visible frames as the measurement endpoint

Add lightweight opt-in diagnostics and a repeatable browser measurement procedure rather than a new analytics backend. Record navigation/open-to-frame, accepted-play-intent-to-frame, source-ready milestones, and seek-request-to-target-frame. Exclude user idle/autoplay-permission waits explicitly. Use displayed-frame callbacks where supported, matching media timestamps to the target within 0.5 seconds; label fallback measurements separately. Record timeout/failure samples rather than dropping them, and avoid recording signed URLs or credentials.

Baseline must first confirm the actual production playback URL and compare its headers to the inspected seed. Compare baseline, lifecycle-only changes, and HLS candidate using the same device/browser/network. Run at least 20 samples per startup/seek case and cache cohort on the primary browser; report raw samples, median, nearest-rank p95, and failures. Cold means browser cache cleared and edge miss verified on an isolated pilot path; warm means browser cache cleared but edge warmed and hit verified. If edge coldness cannot be controlled, label it unknown and do not claim a cold-cache improvement.

Cases: start at zero, resume at minute 60, unbuffered seeks to minutes 30/60/120, paused seek, rapid repeated seeks, source failure and route exit during initialization. Use a recorded stable profile (initial target 10 Mbps, 50 ms added latency) and a constrained profile (1.5 Mbps, 100 ms added latency); record actual tooling and conditions. Smoke-check desktop Chromium, desktop Safari/native HLS, and mobile Safari/Chrome.

Exploratory targets are median startup under 3 seconds and seek under 2 seconds on the stable warm-edge profile, not guaranteed SLAs. Promotion requires improved startup and unbuffered-seek medians over baseline in the stable profile, no p95 regression there, no increase in observed playback failures, and passed compatibility/cache checks. Report constrained/cold results independently. Missing baseline or deployment access means the pilot remains unpromoted, not that performance work is complete.

## Risks / Trade-offs

- [A single seed-URL measurement differs from production] → Verify actual playback response and capture comparable browser timing before attributing causes.
- [History deadline discards a useful late resume] → Prefer immediate stable playback; retain local progress and document the fixed startup decision.
- [Short segments increase object/request count] → Start at four seconds, inspect generated size and request volume, and record operational cost implications before promotion.
- [Encoding alters tracks, duration, or visual quality] → Inspect input, preserve tracks, compare A/V at seek targets, and keep original media.
- [Cache misses and constrained bandwidth still cause waits] → Measure both cohorts and adaptive behavior; do not equate a custom domain with a cache hit.
- [Source teardown emits errors/loading events] → Generation checks and meaningful race tests prevent stale callbacks changing UI.
- [Production access is unavailable] → Complete code and local validation, document the exact missing external step, and leave rollout tasks open.

## Migration Plan

1. Capture baseline and current Natra source configuration; verify source identity and media metadata.
2. Implement lifecycle/resume/recovery changes with focused tests and measure lifecycle-only behavior.
3. Prepare and validate versioned HLS assets, custom-domain delivery, and cache/CORS behavior without changing preferred production playback.
4. Test candidate selection in a controlled environment and collect the comparison report.
5. Promote only Natra by changing existing source priorities after the promotion checks pass; verify playback after rollout.
6. Roll back by restoring the saved MP4 priority. Retain old source rows and objects; revert the frontend change separately if lifecycle behavior regresses.

## Open Questions

- Which existing bucket/custom domain and deployment identity should host the versioned pilot? Resolve from environment inventory during apply; do not invent a production hostname.
- What exact rendition bitrates best preserve the source quality? Resolve through source inspection and pilot measurements within the chosen adaptive-HLS approach.
