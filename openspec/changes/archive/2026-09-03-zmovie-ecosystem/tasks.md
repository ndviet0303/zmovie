## 1. Core Streaming & Multi-Source Backend

- [x] 1.1 Add `EpisodeStreamSource` entity and `PlaybackMilestones` value object to `Episode` aggregate in `ZMovie.Domain/Catalog`.
- [x] 1.2 Update `ICatalogReadStore` and `EfCatalogReadStore` to query and return prioritized multi-source playback payloads (`R2`, `OPhim`, `NguonC`, `Embed`).
- [x] 1.3 Update Catalog persistence configurations and generate EF Core migration for `EpisodeStreamSources`.
- [x] 1.4 Write unit tests in `ZMovie.Domain.Tests` ensuring pure domain invariants and zero wall-clock usage for episode streaming sources.


## 2. Frontend Player Modularization & Experience (Ambilight & Mini-Player)

- [x] 2.1 Refactor `watch/[slug].vue` to reduce script size to ≤ 50 lines according to Frontend Clean Architecture rules.
- [x] 2.2 Create `app/composables/useWatchPlayer.ts` encapsulating multi-source fallback logic, HLS buffer management, and hotkeys.
- [x] 2.3 Create `app/components/PlayerViewport.vue` managing native HLS video and responsive sanitized iframe fallback.
- [x] 2.4 Create `app/components/PlayerAmbilight.vue` executing canvas edge downsampling at 30fps with automatic battery/mobile saver guard.
- [x] 2.5 Create `app/components/PlayerControls.vue` with storyboard thumbnail seekbar tooltip and audio/track selector popover.
- [x] 2.6 Create `app/components/MiniPlayer.vue` docking into a floating bottom-right window on scroll and supporting native Picture-in-Picture.


## 3. Social & Learning (Danmaku & Dual-Sub)

- [x] 3.1 Create `DanmakuHub` SignalR hub in `ZMovie.Infrastructure/Realtime` and `DanmakuComment` entity in `ZMovie.Domain/Engagement`.
- [x] 3.2 Implement `GetTimedDanmakuQuery` and `SendDanmakuCommand` in `ZMovie.Application/Engagement` with time-indexed persistence.
- [x] 3.3 Create `app/composables/useDanmaku.ts` connecting `DanmakuCanvas.vue` to SignalR with local lane collision caching.
- [x] 3.4 Create `app/composables/useDualSub.ts` parsing concurrent primary and secondary WebVTT subtitle tracks.
- [x] 3.5 Create `app/components/DualSubOverlay.vue` with interactive tokenized vocabulary lookup and sentence-repeat hotkey (`R`).


## 4. Monetization (PayOS / SePay 3-Second VIP Webhook)

- [x] 4.1 Remove `builder.Ignore(user => user.VipExpiresAt)` in `UserConfiguration.cs` and map persistent `VipExpiresAt` column.
- [x] 4.2 Create `VipTransaction` aggregate root and `VipActivatedDomainEvent` in `ZMovie.Domain/Identity`.
- [x] 4.3 Implement `ProcessBillingWebhookCommand` in `ZMovie.Application/Billing` with HMAC-SHA256 signature validation and idempotency.
- [x] 4.4 Add PayOS/SePay webhook endpoint `POST /v1/billing/webhook` in `ZMovie.Api` and broadcast instant unlock over SignalR.
- [x] 4.5 Connect `VipCheckoutModal.vue` and `useVipCheckout.ts` to dynamic VietQR generation and live SignalR payment listening.


## 5. Rich Catalog & Automated SEO

- [x] 5.1 Implement `ITmdbClient` and `TmdbEnrichmentService` in `ZMovie.Infrastructure/Catalog` to fetch 4K backdrops, posters, real cast avatars, and YouTube trailers.
- [x] 5.2 Add `TmdbSyncWorker` background job to enrich imported NguonC/OPhim titles.
- [x] 5.3 Enhance `useZMovieSeo.ts` to output Schema.org `Movie` / `VideoObject` JSON-LD and dynamic OpenGraph preview meta tags.


## 6. Engagement & Gamification (TikTok Shorts & Bilibili Badges)

- [x] 6.1 Create `ShortClip` and `UserExpLedger` entities in `ZMovie.Domain/Engagement` calculating levels Lv1 to Lv6.
- [x] 6.2 Implement `GetShortsFeedQuery` in `ZMovie.Application/Engagement` and minimal API endpoint in `ZMovie.Api`.
- [x] 6.3 Build `app/pages/shorts.vue` and `app/composables/useShortsFeed.ts` with vertical scroll-snapping and "Xem phim bản full" deep link.
- [x] 6.4 Create `app/components/UserLevelBadge.vue` rendering animated SVG tier badges across Danmaku, reviews, and user profiles.


## 7. Autonomous Operations (Self-Healing & Telegram Bot)

- [x] 7.1 Implement `StreamHealthMonitorWorker` in `ZMovie.Infrastructure/Operations` executing HTTP HEAD checks on active `.m3u8` streams.
- [x] 7.2 Implement automated failover promotion when a primary stream fails 3 consecutive checks.
- [x] 7.3 Implement `ITelegramOpsNotifier` and Telegram Bot Webhook handler in `ZMovie.Infrastructure/Operations` sending alerts with 1-click crawl retry buttons.
- [x] 7.4 Add anti-leech token generator and middleware validating signed query tokens and Referer headers.


## 8. Verification & Quality Gates

- [x] 8.1 Execute `dotnet test backend/ZMovie.slnx` verifying 100% pass across architecture tests (`ArchitectureDependencyTests`, `DddArchitectureTests`, `DomainTimeConventionTests`).
- [x] 8.2 Verify i18n key parity between `app/i18n/vi.ts` and `app/i18n/en.ts` via `tests/i18n.test.ts`.
- [x] 8.3 Run frontend quality gates: `bun test`, `bun run lint`, `bun run typecheck`, and `bun run build`.

