## Context

ZMovie operates as a modular monolith (.NET 10 Minimal APIs, PostgreSQL 17, Nuxt 4 with Vue 3 and Tailwind CSS). Current capabilities include catalog management, HLS playback via HLS.js, Google auth, and basic watch progress tracking.

To eliminate the risk of broken streams or buffering during live demos, the platform implements a hybrid content strategy:
1. **Self-hosted Cloudflare R2**: Hosts exactly **3 high-reliability demo movies** in full HLS format with WebVTT subtitles, guaranteeing 100% uptime, zero buffering, and full control over player features (hotkeys, quality tracks, Danmaku).
2. **NguonC API Integration**: Ingests broader catalog titles and episodes automatically from NguonC (`https://phim.nguonc.com/api-document`), replacing the initial OPhim scraper.

## Goals / Non-Goals

**Goals:**
- Provide zero-failure video streaming for 3 benchmark demo titles via Cloudflare R2 object storage.
- Support dual-engine video playback on frontend: Native HLS.js player for R2/direct m3u8 streams, and responsive sanitized embed iframe for NguonC streamc embeds.
- Automate ingestion and scheduled updates from NguonC API with rich metadata (actors, directors, country categories).
- Enable resilient realtime group watching via SignalR with sub-second drift compensation.
- Transition AI assistant interactions to low-latency token streaming (<500ms TTFT) without page transitions.
- Implement an autonomous, fault-tolerant background crawler and dynamic VietQR payment loop.

**Non-Goals:**
- Self-hosting hundreds of movies on Cloudflare R2 (storage cost trade-off; only 3 demo titles are hosted on R2).
- Video transcoding farm (R2 demo assets are prepared and uploaded in standard HLS directory structure: `master.m3u8`, quality variants, and `.ts` chunks).
- Complex payment gateway integrations (credit card / Stripe / PayPal); scope is strictly VietQR bank transfers.

## Decisions

### 1. Cloudflare R2 Storage & Demo Benchmark Seeding
- **Decision:** Use Cloudflare R2 (S3-compatible API via `AWSSDK.S3`) to store and serve 3 demo movies via Cloudflare's global edge network (zero egress fees).
- **Configuration:**
  - `R2:AccountId`, `R2:AccessKeyId`, `R2:SecretAccessKey`, `R2:BucketName`, `R2:PublicDomain`
- **Asset Layout in R2:**
  ```text
  /demo-titles/
    ├── title-1/
    │   ├── master.m3u8
    │   ├── 1080p.m3u8, 720p.m3u8, 480p.m3u8
    │   ├── segments/ (segment_000.ts, ...)
    │   ├── vi.vtt, en.vtt
    │   └── poster.webp
    ├── title-2/ ...
    └── title-3/ ...
  ```
- **Seeding Command:** `dotnet run --seed-r2-demo` automatically upserts these 3 titles into `CatalogDbContext` with `Featured = true` and directs their episode streams to the R2 CDN endpoints.

### 2. Dual-Engine Player Architecture: HLS.js vs. Responsive Embed
- **Decision:** Implement adaptive engine selection in `watch/[slug].vue`:
  - **Native HLS Mode (`isNativeStream = true`):** Triggered when the stream URL ends in `.m3u8` or belongs to R2 / direct video sources. Mounts `<video>` controlled by HLS.js, enabling hotkeys, subtitle track selector, audio mute, and the Danmaku Canvas overlay.
  - **Embed Iframe Mode (`isNativeStream = false`):** Triggered when the source is an external embed URL (e.g. `https://embed.streamc.xyz/...` from NguonC). Mounts a sandboxed responsive `<iframe>` with `allow="autoplay; fullscreen; encrypted-media"`, while retaining native UI around it (episode selector, report modal, watchlist, comments).

```
┌─────────────────────────────────────────────────────────────┐
│                 Dual-Engine Video Player Flow               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                    Episode Stream URL                       │
│                            │                                │
│              ┌─────────────┴─────────────┐                  │
│              ▼                           ▼                  │
│     [.m3u8 / R2 CDN URL]     [External Embed URL (StreamC)] │
│              │                           │                  │
│              ▼                           ▼                  │
│      [Native HLS.js Engine]     [Responsive Iframe Player]  │
│      ├── Custom Hotkeys Bus     ├── Native Provider Controls│
│      ├── WebVTT Subtitles Menu  └── Retained Shell Controls │
│      └── Canvas Danmaku Layer        (Episodes, Report, Bio)│
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3. NguonC Catalog Ingestion vs. OPhim
- **Decision:** Implement `NguonCCatalogImporter` targeting NguonC REST API (`https://phim.nguonc.com/api`):
  - List endpoint: `/api/films/phim-moi-cap-nhat?page={page}`
  - Detail endpoint: `/api/film/{slug}`
- **Field Mapping:**
  - `movie.name` $\rightarrow$ `Title.VietnameseTitle`
  - `movie.original_name` $\rightarrow$ `Title.EnglishTitle`
  - `movie.description` $\rightarrow$ `Title.VietnameseSynopsis` (HTML stripped)
  - `movie.director` $\rightarrow$ `Title.Directors`
  - `movie.casts` $\rightarrow$ `Title.Actors`
  - `movie.category` $\rightarrow$ parsed into `Genre` and `Country`
  - `movie.episodes.items` $\rightarrow$ `Episode.Name`, `Episode.Number`, `Episode.HlsUrl` (stores embed/stream URL)
  - `movie.thumb_url`, `movie.poster_url` $\rightarrow$ `Title.PosterUrl`

### 4. Danmaku Rendering: HTML5 Canvas Overlay
- **Decision:** Use an HTML5 `<canvas>` element layered directly above the `<video>` element in Native HLS mode, rendered via `requestAnimationFrame`. Text items animate across the viewport based on `currentTime`.

### 5. Realtime Watch Party: SignalR Hub with Drift Correction
- **Decision:** Implement `WatchPartyHub` with room groups and 3-tier drift compensation:
  - $|\Delta t| < 0.5s$: In-sync. Do nothing.
  - $0.5s \le |\Delta t| \le 2.0s$: Soft catch-up via temporary `playbackRate` adjustment ($1.05\times / 0.95\times$).
  - $|\Delta t| > 2.0s$: Hard seek to host position.

### 6. AI Assistant: Server-Sent Events (SSE) via IAsyncEnumerable
- **Decision:** Expose `/v1/assistant/chat/stream` returning `IAsyncEnumerable<string>` with content type `text/event-stream`, consumed by `FloatingAssistantWidget.vue`.

### 7. Crawler Automation: .NET BackgroundService with PeriodicTimer
- **Decision:** Encapsulate `NguonCCatalogImporter` into `IHostedService` (`CatalogCrawlerBackgroundWorker`) using `PeriodicTimer`, with configuration stored in `crawler_configs`.

### 8. Dynamic VietQR Billing & Webhook Processing
- **Decision:** Generate VietQR Napas 247 dynamic QR codes with unique order codes (`ZM_VIP_...`). Webhook receiver validates secret header, checks transaction idempotency, and extends `User.VipExpiresAt`.

## Risks / Trade-offs

| Risk | Impact | Mitigation Strategy |
| --- | --- | --- |
| **External NguonC link downtime or iframe ads** | Poor viewer experience on third-party titles | 3 core demo titles are 100% self-hosted on Cloudflare R2 to guarantee flawless presentations; embed iframe is sandboxed. |
| **R2 egress or storage costs** | Unexpected cloud bill | Limited strictly to 3 curated benchmark demo movies; Cloudflare R2 has zero data egress fees. |
| **Danmaku comment lag on low-end devices** | Video stuttering | Canvas rendering with requestAnimationFrame, offscreen text pooling, max 6 parallel tracks, user toggle to disable. |
| **NguonC rate limiting during crawl** | Scheduler worker delayed | Ingest pages with 300-500ms delay jitter; detail requests throttled to 2 concurrent workers. |

## Migration Plan

1. **Database Schema Additions**:
   - `CatalogDbContext`: Add columns `actors` (text[]), `directors` (text[]), `country` (varchar), `trailer_url` (varchar), `is_r2_hosted` (boolean).
   - `EngagementDbContext`: Add tables `movie_reports` and `danmaku_comments`.
   - `IdentityDbContext`: Add columns `vip_expires_at` (timestamp with time zone) and `subscription_tier` to `users`. Add table `payment_orders`.
   - `NotificationDbContext`: Add `notifications` table.
2. **Backward Compatibility**:
   - Existing endpoints remain untouched.
   - Dual-engine player handles both direct m3u8 streams and external embed URLs seamlessly.
