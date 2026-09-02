## Why

ZMovie aims to deliver a top-tier streaming ecosystem combining enterprise-grade video playback, high-converting monetization, immersive community engagement, and autonomous operations. While foundational elements (such as single-stream HLS playback, basic crawler imports, and demo VIP modal UI) exist, the platform lacks multi-source streaming failover, cinematic player enhancements (Ambilight, Dual-Sub, Storyboard Thumbnails), instant automated payment reconciliation (PayOS/SePay 3-second VIP activation), autonomous healing for dead streams, TikTok-style movie shorts, and rich TMDB 4K metadata enrichment.

Implementing this comprehensive ecosystem elevates ZMovie from a standard video catalog into a state-of-the-art, self-operating streaming platform with high viewer retention and monetization velocity.

## What Changes

The ZMovie Ecosystem introduces systemic capabilities across 7 key pillars:

1. **Core Streaming**:
   - Multi-source failover engine: dynamic resolution across R2 VIP, OPhim, NguonC, and embed backups with zero interruption.
   - Native HLS streaming optimizations with adaptive bitrate switching.
   - Precise Skip Intro / Skip Outro timestamp skipping.
   - Multi-Audio support (Vietsub, Thuyết minh, Lồng tiếng, Original track).

2. **Player Experience**:
   - Real-time Ambilight glow sampling video canvas edges to project atmospheric background lighting.
   - Storyboard thumbnail preview scrubbing across seekbar.
   - Responsive floating Mini-Player & native Picture-in-Picture mode on scroll.

3. **Social & Learning**:
   - Low-latency real-time Danmaku comments over SignalR with time-indexed Postgres/Redis caching and lane collision avoidance.
   - Dual-Subtitle language learning engine with synchronous dual cues, hover vocabulary lookup, and sentence looping hotkeys.
   - Enhanced Watch Party synchronization room with host playback controls and live chat.

4. **Rich Catalog**:
   - TMDB 4K Metadata Enricher pulling official 4K backdrops, posters, cast/director profiles with avatars, and official YouTube trailers.
   - Automated SEO engine outputting dynamic JSON-LD Schema (`Movie`, `VideoObject`), dynamic OpenGraph cards, and automated sitemaps.

5. **Engagement & Gamification**:
   - TikTok-style Movie Shorts feed (`/shorts`) with vertical snapping, auto-play reels, and direct "Watch Full Movie" deep links.
   - Bilibili-style Gamification with user levels (Lv1 to Lv6), EXP accumulation (watch time, reviews, danmaku), and animated SVG badges.

6. **Monetization (VIP 3s)**:
   - Automated 3-second VietQR payment reconciliation via PayOS / SePay webhooks.
   - Persistent `VipExpiresAt` mapping in PostgreSQL (`IdentityDbContext`).
   - Real-time VIP activation push via SignalR to unlock premium perks instantly without page refresh.

7. **Autonomous Operations**:
   - Self-healing crawler bot actively probing `.m3u8` stream health (HTTP 200/403/404/CORS) and auto-switching dead links to healthy sources.
   - Anti-leech protection with tokenized short-lived streaming URLs, Referer/Origin enforcement, and IP rate-limiting.
   - Telegram Alert Bot notifying ops channels on broken streams, crawl failures, and user reports with 1-click resolution actions.

## Capabilities

### Modified Capabilities
- `playback/player-enhancements`: Adds multi-source failover (.m3u8 + embed), Skip Intro/Outro timestamps, multi-audio selection, Ambilight glow canvas, storyboard thumbnail scrubbing, floating mini-player, and dual-subtitle learning overlays.
- `operations/admin-automation-and-vip`: Adds automated VietQR webhook processing (PayOS/SePay) with HMAC verification, un-ignores persistent `VipExpiresAt`, implements self-healing stream health worker, anti-leech tokenized playback routes, and Telegram ops alert bot integration.
- `catalog/enriched-discovery`: Adds TMDB 4K metadata enrichment (cast/director avatars, official trailers, 4K artwork) and automated SEO JSON-LD/OpenGraph generation.
- `engagement/history-and-reports`: Adds TikTok-style vertical Movie Shorts feed (`/shorts`) and Bilibili-style user gamification (EXP calculation, levels Lv1-Lv6, dynamic badges).
- `community/watch-party`: Adds real-time Danmaku SignalR broadcasting, time-indexed playback storage, and synchronized watch party rooms.

## Impact

- **Backend Architecture**:
  - `ZMovie.Domain`: Pure domain entities and Value Objects (`EpisodeSource`, `PlaybackMilestones`, `VipTransaction`, `UserLevel`, `Badge`, `DanmakuBullet`), pure factory methods, domain events (`VipActivatedDomainEvent`, `StreamHealthFailedDomainEvent`, `UserExpGainedDomainEvent`), zero wall-clock dependencies.
  - `ZMovie.Application`: MediatR commands/queries for billing webhooks, Danmaku pagination, TMDB synchronization, shorts feed, and stream health checks.
  - `ZMovie.Infrastructure`: PayOS/SePay webhook adapters, TMDB API client, Telegram Bot notification adapter, EF Core configurations (enabling `VipExpiresAt`, adding `VipTransactions`, `DanmakuComments`, `UserExpRecords`), SignalR Hubs.
  - `ZMovie.Api`: Minimal API endpoints for billing webhooks, shorts feed, Danmaku streaming, and Telegram webhook receiver.
- **Frontend Architecture**:
  - Refactoring `watch/[slug].vue` to strictly adhere to `<= 50` LoC script rule by extracting UI components (`PlayerAmbilight.vue`, `PlayerControls.vue`, `DualSubOverlay.vue`, `MiniPlayer.vue`) and composables (`useWatchPlayer.ts`, `useDanmaku.ts`, `useDualSub.ts`).
  - New pages and components: `/shorts` page (`useShortsFeed.ts`), Gamification badge components (`UserLevelBadge.vue`), PayOS checkout integration in `useVipCheckout.ts`.
  - Localization: Adding all ecosystem copy keys to `app/i18n/vi.ts` and `app/i18n/en.ts` ensuring 100% type parity with `Messages`.
- **Database**:
  - PostgreSQL schema migrations in `CatalogDbContext`, `IdentityDbContext`, `EngagementDbContext`, and `OperationsDbContext`.
