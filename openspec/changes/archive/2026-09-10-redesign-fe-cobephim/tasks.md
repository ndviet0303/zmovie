## 1. Design System & Theme Foundations

- [x] 1.1 Update `assets/css/main.css` with CôBéPhim color tokens (`#0f111a`, `#191b24`, `#202331`, `#ffd875`), card shadows, and badge styles
- [x] 1.2 Add CôBePhim SVG logo and animated badge assets in `public/` or as inline Vue components

## 2. Core Shell Components (Header, Drawer & Footer)

- [x] 2.1 Build `AppDrawer.vue` (Slide-out drawer with "Thành viên", app download banner, and expandable navigation categories)
- [x] 2.2 Refactor `AppNavbar.vue` to minimalist header (Hamburger menu, CôBePhim brand logo, search trigger, and drawer integration)
- [x] 2.3 Build `AppFooter.vue` with Vietnamese sovereignty banner ("★ Hoàng Sa & Trường Sa là của Việt Nam!"), social media channels, links, and copyright

## 3. Shared Presentation Components & Cards

- [x] 3.1 Build `CobeMovieCard.vue` supporting 2:3 vertical posters and 16:9 horizontal backdrops with badge overlays (`Song ngữ`, `4K`, `P.Đề`, `T.Minh`, and episode progress)
- [x] 3.2 Build `CobeTopicCards.vue` for the "Bạn đang quan tâm gì?" horizontal gradient highlight cards
- [x] 3.3 Build `CobeLeaderboard.vue` for the 1-to-10 weekly ranked leaderboard with custom outline numbers

## 4. Home Page (`/`) Redesign

- [x] 4.1 Implement full-bleed hero banner with title metadata, genres, circular action buttons (Play, Favorite, Info), and interactive thumbnail slider
- [x] 4.2 Integrate "Bạn đang quan tâm gì?" topic cards section
- [x] 4.3 Redesign movie rows to support 16:9 Song Ngữ cards and 2:3 vertical poster rows matching `01_home_fullpage.webp`
- [x] 4.4 Integrate the shared `AppFooter.vue` at the bottom of the home page

## 5. Catalog & Filter View (`/browse`) Redesign

- [x] 5.1 Implement collapsible multi-tier filter matrix (`Quốc gia`, `Loại phim`, `Xếp hạng`, `Thể loại`) with pill toggle states
- [x] 5.2 Build responsive 6-column movie card grid (2-column on mobile) using `CobeMovieCard.vue`
- [x] 5.3 Implement search view tab switcher (`[Phim]` and `[Diễn viên]`) matching `02_search_results.webp`

## 6. Movie Detail View (`/movies/[slug]`) Redesign

- [x] 6.1 Implement two-column layout with floating poster, title metadata, broadcast status, synopsis, and circular cast avatars
- [x] 6.2 Implement right-hand action bar (`▶ Xem Ngay`, favorites, share, review rating) and tabs (`Tập phim`, `Gallery`, `Diễn viên`, `Đề xuất`)
- [x] 6.3 Build episode selector controls (`Phần 1 ▾`, audio track toggles, compact switch, and episode button grid)
- [x] 6.4 Implement comments and review composer matching `03_movie_detail_fullpage.webp`
- [x] 6.5 Integrate `CobeLeaderboard.vue` into the left sidebar

## 7. Watch View (`/watch/[slug]`) Redesign

- [x] 7.1 Implement watch top bar (`‹ Xem phim [Title] - Tập [N]`)
- [x] 7.2 Implement under-player control toolbar (`Chuyển tập [ON]`, `Bỏ qua giới thiệu [OFF]`, `Rạp phim [OFF]`, `Chia sẻ`, `Xem chung`, `Báo lỗi`)
- [x] 7.3 Implement chapter preview thumbnails strip below player
- [x] 7.4 Implement meme error state ("CÓ BIẾN RỒI - Hãy thử refresh lại!") for playback load failure
- [x] 7.5 Redesign episode selection grid and integrate recommendations column and comments

## 8. Mobile Responsiveness & Polish

- [x] 8.1 Validate and tune responsive behavior on mobile (drawer animations, touch targets, 2-col grids)
- [x] 8.2 Run build check (`bun run build` / `npm run build`) and typecheck to ensure zero regressions
