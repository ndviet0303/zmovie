## Context

The frontend is a Nuxt 4 (Vue 3) single-page application utilizing Tailwind CSS v4 and `@lucide/vue`. The current UI implements a conventional slate-dark theme. High-fidelity visual references and extracted design tokens exist in `cobephim_screenshots` and `cobephim-ui-bundle.html`. Backend endpoints for titles, episodes, streaming, user libraries, and discovery remain operational and do not require modification.

## Goals / Non-Goals

**Goals:**
- Faithfully replicate the visual hierarchy, component shapes, spacing, and micro-interactions of CôBéPhim across Desktop and Mobile viewports.
- Refactor global theme variables to match the CôBéPhim palette (`#0f111a`, `#191b24`, `#ffd875`).
- Implement the minimalist sticky header with slide-out drawer navigation for all devices.
- Build the rich Home screen: Hero spotlight carousel, "Bạn đang quan tâm gì?" gradient topic cards, 16:9 and 2:3 card rows with badge overlays (`Song ngữ`, `4K`, `P.Đề`, `T.Minh`).
- Build the multi-tier expandable filter panel in the catalog browsing view.
- Re-architect the Movie Detail view into a 2-column layout with weekly top 10 leaderboard, circular cast avatars, episode selector, and comments.
- Re-architect the Watch view with under-player control toolbar, meme error fallback, chapter previews, and quick episode grid.
- Implement the Vietnamese sovereignty footer across all public views.

**Non-Goals:**
- Modifying backend .NET APIs or PostgreSQL database schemas.
- Re-implementing streaming playback engines from scratch (preserves existing HLS.js and iframe fallback).
- Modifying the `/admin` back-office dashboard views.

## Decisions

### 1. Palette & CSS Token Strategy
- **Choice**: Extend `assets/css/main.css` `@theme inline` and CSS variables with CôBéPhim's exact color tokens:
  - Background: `#0f111a`
  - Card/Surface layers: `#191b24`, `#202331`, `#282b3a`
  - Primary accent: `#ffd875` / `#ffd43b`
  - Badge colors: Blue `#1667cf` (Song ngữ), Green `#10b981` (Thuyết minh), Slate `#374151` (Phụ đề), Amber `#ff9800` (4K).
- **Alternative considered**: Maintaining existing ZMovie orange palette (`#f89300`). Rejected because user specifically requested the exact CôBéPhim design.

### 2. Header and Slide-out Navigation Drawer
- **Choice**: Replace desktop horizontal links with a unified, minimalist header:
  - Left: Hamburger icon button.
  - Center/Left: CôBePhim brand logo.
  - Right: Search button.
  - When Hamburger is clicked, a slide-out drawer appears with dark overlay, containing user account status, mobile app promo card, and expandable links (`Thể loại`, `Phim Lẻ`, `Phim Bộ`, `Quốc gia`, `Thêm`).
- **Alternative considered**: Keeping horizontal text links on desktop and drawer only on mobile. Rejected because CôBéPhim's desktop layout specifically uses the minimalist header with drawer to maximize visual focus on movie artwork.

### 3. Reusable Component Decomposition
- **Choice**: Create modular components:
  - `CobeDrawer.vue`: The slide-out navigation drawer with smooth entrance/exit transitions.
  - `CobeMovieCard.vue`: Supports both `vertical` (2:3) and `horizontal` (16:9) modes, with badge overlays (`Song ngữ`, `4K`, `P.Đề`, `T.Minh`, episode counts) and zoom effects.
  - `CobeTopicCards.vue`: Gradient highlight cards for "Bạn đang quan tâm gì?".
  - `CobeFooter.vue`: Sovereignty statement, CôBePhim logo, social links, and SEO text.
  - `CobeLeaderboard.vue`: Top 10 weekly ranking list with outline rank numbers (1 to 10).
- **Alternative considered**: Monolithic page templates. Rejected for maintainability and code reuse across Home, Browse, and Detail views.

### 4. Detail Page 2-Column Architecture
- **Choice**:
  - Left column (approx 340px): Floating poster, movie title, metadata chips, synopsis, production details, cast avatars, and weekly top 10 leaderboard.
  - Right column: Yellow `▶ Xem Ngay` button, action buttons, episode tabs (`Tập phim`, `Gallery`, `Diễn viên`, `Đề xuất`), server/subtitle filters, episode grid, and comments.

### 5. Watch Page Experience Toolbar
- **Choice**:
  - Keep existing video player engine (HLS.js + Cloudflare R2 / embed iframe).
  - Wrap player with CôBéPhim controls toolbar: Autoplay next (`Chuyển tập [ON]`), Skip intro (`Bỏ qua giới thiệu [OFF]`), Theater mode (`Rạp phim [OFF]`), Share, Watch party (`Xem chung`), and Issue reporting (`Báo lỗi`).
  - Chapter preview thumbnails below the toolbar.
  - Themed meme error state ("CÓ BIẾN RỒI - Hãy thử refresh lại!").

## Risks / Trade-offs

- **[Risk] Title metadata missing Vietnamese/English duality or backdrops**:
  - *Mitigation*: Fall back gracefully to `title.title` if original title is absent; fall back to poster with gradient blur if wide backdrop is unavailable.
- **[Risk] Viewport responsiveness on narrow mobile screens (360px - 414px)**:
  - *Mitigation*: Use 2-column grid for movie cards on mobile with clamped typography and horizontal scroll for multi-tier filters.
