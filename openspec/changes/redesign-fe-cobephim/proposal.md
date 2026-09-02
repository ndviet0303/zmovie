## Why

The current frontend UI of ZMovie employs a generic, dark-mode slate theme with a standard horizontal desktop navbar and basic cards that lack the visual richness, density, and immersive streaming feel of top Vietnamese movie platforms. Adopting the exact UI design system of CôBéPhim (RoPhim)—documented in `cobephim_screenshots` and `cobephim-ui-bundle.html`—elevates user engagement with an unmistakable cinematic brand identity, seamless drawer navigation, rich metadata badges, multi-tier catalog filtering, and a feature-complete watch experience.

## What Changes

- **Brand & Theme Tokens**: Introduce the CôBéPhim dark navy palette (`#0f111a`, `#191b24`, `#202331`), warm gold primary (`#ffd875` / `#ffd43b`), badge colors (`Song ngữ`, `Thuyết minh`, `Phụ đề`, `4K`), and signature playful logo.
- **Header & Navigation Drawer**: Replace the desktop horizontal text navbar with a minimalist sticky header containing only Hamburger menu, CôBePhim brand logo, and Search icon. Add a slide-out drawer featuring a "Thành viên" user button, app promo banner, and expandable navigation dropdowns (`Thể loại`, `Phim Lẻ`, `Phim Bộ`, `Quốc gia`, `Thêm`).
- **Home Screen Overhaul**:
  - Full-bleed hero banner with original titles, metadata pills (`IMDb`, `T16`, `2026`, `Phần 1`, `Tập 10`), genres, circular action buttons (Play, Favorite, Info), and an interactive thumbnail switcher.
  - "Bạn đang quan tâm gì?" horizontal topic cards (`Top IMDb`, `Thuyết Minh`, `Phim 4K`, `Lồng Tiếng Cực Mạnh`, `Netflix`, `TVB`, `+4 chủ đề`).
  - Dedicated sections supporting both 16:9 backdrop cards (`Phim Song Ngữ`) and 2:3 vertical poster cards with comprehensive badge overlays (`Song ngữ`, `4K`, `P.Đề`, `T.Minh`).
- **Catalog & Search Filter Experience**:
  - Collapsible multi-tier filter matrix (`Quốc gia`, `Loại phim`, `Xếp hạng`, `Thể loại`) with pill toggle states.
  - Responsive 6-column movie grid (2-column on mobile) with glowing hover states.
  - Search view with `[Phim]` and `[Diễn viên]` filter tabs.
- **Movie Detail Screen Redesign**:
  - Two-column layout: Left column with rounded floating poster, titles, metadata badges, broadcast status, synopsis, production details, circular cast avatars, and a 1-to-10 weekly leaderboard ("Top phim tuần này").
  - Right column with yellow pill `▶ Xem Ngay` button, action icons (`♥`, `+`, `✈`, `💬`, rating badge `★ 0 Đánh giá`), tabs (`Tập phim`, `Gallery`, `Diễn viên`, `Đề xuất`), audio/subtitle selectors (`Phụ đề #1`, `Song ngữ`, `Thuyết Minh #1`), episode grid, and a rich commenting interface.
- **Video Player & Watch Experience**:
  - Top header `‹ Xem phim [Tên Phim] - Tập [N]`.
  - Player container with humor fallback state ("CÓ BIẾN RỒI - Hãy thử refresh lại!").
  - Under-player toolbar: Autoplay next (`Chuyển tập`), Skip intro (`Bỏ qua giới thiệu`), Theater mode (`Rạp phim`), Share, Watch party (`Xem chung`), and Issue reporting (`Báo lỗi`).
  - Chapter thumbnail strip, episode selector, community Discord banner, cast list, and recommendations.
- **Vietnamese Sovereignty Footer**:
  - Red Sovereignty banner: *"★ Hoàng Sa & Trường Sa là của Việt Nam!"*.
  - CôBePhim logo, social links (Telegram, Discord, X, Facebook, TikTok, YouTube), legal and informational links, platform description, and Telegram contact.

## Capabilities

### New Capabilities
- `ui/cobephim-layout-and-design-system`: The CôBéPhim global design tokens, typography, minimalist header, slide-out drawer menu, topic card system, and Vietnamese sovereignty footer.

### Modified Capabilities
- `catalog/enriched-discovery`: Movie cards, catalog browsing, search result presentation, and movie detail page architecture updated to CôBéPhim's multi-tier filter panel, badge system, and 2-column detail layout.
- `playback/player-enhancements`: Watch page layout, player controls toolbar, meme error state, episode selector, and community/recommendations sections updated to CôBéPhim specifications.

## Impact

- **Frontend Codebase**:
  - `frontend/assets/css/main.css`: Theme tokens and utility classes updated to CôBéPhim specs.
  - `frontend/app/components/AppNavbar.vue`: Refactored to minimal header + slide-out drawer.
  - `frontend/app/components/AppFooter.vue`: New shared footer component matching CôBéPhim.
  - `frontend/app/pages/index.vue`: Redesigned hero slider, topic cards, and movie rows.
  - `frontend/app/pages/browse.vue`: Redesigned collapsible filter matrix and movie card grid.
  - `frontend/app/pages/movies/[slug].vue`: Redesigned into CôBéPhim 2-column layout.
  - `frontend/app/pages/watch/[slug].vue`: Redesigned player toolbar, chapters, and episode grid.
- **APIs and Backend**: No backend breaking changes. All existing .NET endpoints, R2 playback URLs, NguonC catalog data, and user authentication remain compatible.
