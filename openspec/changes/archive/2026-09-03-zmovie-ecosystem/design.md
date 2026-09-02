## Context

See `proposal.md` for motivation and background context.

The ZMovie codebase is structured as a .NET 10 Modular Monolith backend following strict Domain-Driven Design (DDD) rules, and a Nuxt 4 (Vue 3, TypeScript, Bun) frontend following 3-layer Clean Architecture. Currently:
- Catalog playback in `EfCatalogReadStore` provides only a single `HlsUrl` per episode.
- `watch/[slug].vue` is an oversized single-file component (~1,460 lines) containing mixed concerns (HLS video element, Danmaku state, subtitle parsing, reporting modal, and inline types).
- Identity aggregate root `User` in backend has `builder.Ignore(user => user.VipExpiresAt)`, preventing persistent VIP activation.
- Background crawling is limited to basic page scraping without automated playback health validation or ops alerting.

## Goals / Non-Goals

**Goals:**
- Architect a resilient multi-source playback failover engine with high-performance Ambilight canvas sampling, Dual-Sub language learning, and frame scrubbing.
- Refactor the frontend player experience into thin presentation components (≤ 50 LoC script) backed by cohesive composables (`useWatchPlayer`, `useDanmaku`, `useDualSub`).
- Enable secure, idempotent 3-second VIP activation via PayOS/SePay webhook processing, un-ignoring `VipExpiresAt` and broadcasting instant unlock notifications via SignalR.
- Build an autonomous self-healing worker for stream health verification and Telegram ops bot notification with 1-click action triggers.
- Introduce TikTok-style vertical video shorts feed (`/shorts`) and a Bilibili-style gamification system (Lv1-Lv6 badges and EXP).
- Enrich catalog titles with TMDB 4K artwork and automated Schema.org / OpenGraph SEO metadata.

**Non-Goals:**
- Custom video transcoding on backend (ZMovie consumes external HLS streams and Cloudflare R2 pre-encoded HLS segments).
- Building an in-house payment gateway (leverages verified VietQR providers PayOS and SePay).
- Native mobile app compilation (focus remains on responsive Nuxt 4 PWA/web app).

## Decisions

### 1. Multi-Source Streaming & Player Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Player Orchestration                    │
├─────────────────────────────────────────────────────────────┤
│  1. Primary HLS Source (Cloudflare R2 / VIP CDN)            │
│       │ (onError: 403, 404, manifest stall)                 │
│       ▼                                                     │
│  2. Secondary HLS Source (OPhim / NguonC Direct M3U8)       │
│       │ (onError)                                           │
│       ▼                                                     │
│  3. Fallback Embed Source (Iframe Embed Provider)           │
└─────────────────────────────────────────────────────────────┘
```

- **Domain Model**:
  - Introduce `EpisodeStreamSource` entity to `Episode` aggregate:
    `public sealed class EpisodeStreamSource(EpisodeSourceId id, StreamProvider provider, string url, StreamFormat format, int priority, bool isActive)`
  - Introduce Value Object `PlaybackMilestones(int? IntroStart, int? IntroEnd, int? OutroStart, int? OutroEnd)`.
- **Frontend Modularization**:
  - Decompose `watch/[slug].vue` into focused presentation components:
    - `PlayerViewport.vue`: hosts `<video>` and fallback `<iframe>`
    - `PlayerAmbilight.vue`: executes canvas pixel downsampling and ambient glow CSS projection
    - `PlayerControls.vue`: hotkeys, scrub bar with storyboard tooltip, audio/quality popovers
    - `DualSubOverlay.vue`: handles dual WebVTT cue rendering and vocabulary definition popover
    - `MiniPlayer.vue`: docks into lower-right floating frame on scroll past fold
  - Encapsulate business logic in `app/composables/useWatchPlayer.ts`, `app/composables/useDualSub.ts`, and `app/composables/useDanmaku.ts`.

### 2. Player Experience: High-Performance Ambilight

- **Approach**: An `OffscreenCanvas` (or hidden `<canvas width="16" height="9">`) samples video frame colors via `requestAnimationFrame` at throttled intervals (20–30fps).
- **GPU Acceleration**: Calculate average edge colors (top, bottom, left, right) and inject dynamic CSS variables `--ambilight-glow-left`, `--ambilight-glow-right`, etc. into player container background box-shadows.
- **Battery Saver Guard**: Read `navigator.getBattery()` and detect mobile touch devices to automatically downgrade or disable ambilight when power is constrained.

### 3. Dual-Subtitle Language Learning Engine

- **Approach**: Parse primary and secondary WebVTT subtitle files via clientside parser into a synchronized cue array.
- **Interactive Vocabulary Lookup**: Each word in foreign subtitle cues is wrapped in an interactive token `<span class="vocab-word" @click="lookupWord(word)">`. Clicking fetches concise dictionary definitions and audio pronunciations from a lightweight local dictionary cache or translation endpoint without interrupting video context.

### 4. Billing & 3-Second VIP Webhook Processing

```
User (Checkout) ──▶ Generates VietQR (ZM_VIP_{OrderId})
                         │
                    User Transfers via Bank App
                         │
                         ▼
             Bank ──▶ PayOS / SePay Gateway
                         │
                         ▼ (POST /v1/billing/webhook/payos)
                ZMovie.Api (HMAC-SHA256 Signature Check)
                         │
                         ▼
             CreateVipPaymentWebhookCommand
                         │ (Idempotent Transaction Check)
                         ▼
             IdentityDbContext.Users.ExtendVip(...)
                         │
                         ▼
             Raise VipActivatedDomainEvent
                         │
                         ▼
             SignalR Hub: Clients.User(userId).SendAsync("VipActivated")
```

- **Persistence Fix**:
  - Remove `builder.Ignore(user => user.VipExpiresAt)` in `UserConfiguration.cs`.
  - Map `VipExpiresAt` as `timestamp with time zone` column in PostgreSQL.
  - Create `VipTransaction` aggregate root in `IdentityDbContext` storing `OrderId`, `Amount`, `Provider`, `Status`, `TransactionDate`, and `RawWebhookPayload` for auditing and idempotency.
- **Security & Idempotency**:
  - Verify HMAC-SHA256 signatures with configured gateway webhook secrets.
  - Wrap processing in a transaction: if `VipTransaction` with given provider transaction reference already exists, return HTTP 200 OK immediately without duplicate subscription extension.

### 5. Autonomous Ops: Self-Healing Bot & Telegram Alerts

- **Self-Healing Bot Worker**:
  - Hosted as a background service (`IHostedService` in `ZMovie.Infrastructure`) executing hourly or on-demand.
  - Emits HTTP HEAD requests to primary `.m3u8` URLs with a 5-second timeout.
  - If a stream fails 3 times successively:
    1. Sets `IsActive = false` on the failed `EpisodeStreamSource`.
    2. Promotes next priority source to active.
    3. Emits `StreamHealthDegradedDomainEvent`.
- **Telegram Bot Integration**:
  - Sends formatted Markdown alerts to ops group chat via `TelegramBotClient`:
    `🚨 *STREAM DOWN ALERT* | Title: {MovieName} | Ep: {EpisodeNumber} | Error: {StatusCode}`
  - Includes inline keyboard with callback data: `action:retry_crawl:{TitleSlug}:{EpisodeNumber}`.

### 6. Rich Catalog: TMDB 4K Enrichment & Automated SEO

- **TMDB Client Adapter**:
  - Integrates `ITmdbClient` in `ZMovie.Infrastructure/Catalog`.
  - Queries `/3/search/movie` and `/3/movie/{id}?append_to_response=credits,videos`.
  - Enriches `Title` entity with `Backdrop4kUrl`, `TmdbRating`, `TmdbId`, YouTube trailer key, and structured `Actors` with profile paths.
- **Dynamic SEO Engine**:
  - Nuxt composable `useZMovieSeo.ts` enhanced with Schema.org `Movie` and `VideoObject` JSON-LD script blocks, dynamic `og:image`, and canonical URLs.

### 7. Engagement: TikTok Shorts Feed & Bilibili Gamification

- **Shorts Feed (`/shorts`)**:
  - Nuxt page with CSS scroll-snap (`scroll-snap-type: y mandatory`) rendering virtualized video cards.
  - IntersectionObserver pauses offscreen clips and autoplays active clip muted by default.
  - Quick action bar: Like, Add to Watchlist, Share, and "Xem phim bản full" deep link.
- **Gamification & Badges**:
  - Domain entity `UserExpLedger` tracking activity points (Watching: 10 EXP/15 mins, Review: 50 EXP, Danmaku: 2 EXP).
  - Level thresholds: Level 1 (0 EXP) to Level 6 (10,000 EXP).
  - Unlocked badges rendered as lightweight SVG components across player Danmaku, reviews, and user profile.

## Risks / Trade-offs

- **[Risk] High canvas sampling CPU usage in Ambilight**
  → *Mitigation*: Downscale frame to 16x9 before sampling, throttle render loop to 24-30fps, disable automatically on battery-saver or mobile devices.
- **[Risk] Danmaku high throughput causing UI lag**
  → *Mitigation*: Canvas-based rendering with batch text draws, fixed 8 lanes with collision tracking, and local chunking of comments by playback second.
- **[Risk] Webhook forgery or duplicate payments**
  → *Mitigation*: Strict HMAC-SHA256 signature verification and unique transaction idempotency keys in PostgreSQL.
- **[Risk] External CDN CORS blocks on HLS manifests**
  → *Mitigation*: Self-healing bot detects CORS failures and flags stream for backend proxy fallback or embed mode.

## Migration Plan

1. Database Migrations:
   - Add `VipExpiresAt` and `VipTransactions` to Identity context.
   - Add `EpisodeStreamSources` and `PlaybackMilestones` to Catalog context.
   - Add `DanmakuComments` and `UserExpLedgers` to Engagement context.
2. Deployment sequence:
   - Deploy backend API with webhook routes and new SignalR hub endpoints.
   - Run seed & TMDB enrichment worker.
   - Deploy Nuxt 4 frontend with decomposed player components and `/shorts` route.
3. Rollback:
   - Database additions are non-destructive (new tables and optional nullable columns).
   - If frontend player encounters regressions, embed player fallback activates automatically.
