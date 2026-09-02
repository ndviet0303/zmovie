## 1. Foundation — Types & Infrastructure

- [x] 1.1 Tạo `app/types/api-fetch.ts` — export type `ApiFetch` từ `ofetch`
- [x] 1.2 Tạo `app/types/catalog.ts` — re-export `TitleSummary`, `TitleDetail`, `TitleListResponse`, `PlaybackResponse`, `PlaybackEpisode` từ `api.d.ts`; thêm `SortOrder`, `TitleFilter`
- [x] 1.3 Tạo `app/types/discovery.ts` — define `HomeResponse`, `TopTitle`, `TopPeriod`, `ContinueWatching`, `PersonalizedDiscovery` (move từ index.vue)
- [x] 1.4 Tạo `app/types/library.ts` — define `History`, `Library` (move từ my-list.vue)
- [x] 1.5 Cập nhật `app/types/admin.ts` — giữ nguyên nhưng thêm re-export convenience aliases nếu cần
- [x] 1.6 Tạo `app/types/auth.ts` — move `SessionUser`, `UserRole` từ `admin.ts`; `admin.ts` re-import từ đây

## 2. i18n System

- [x] 2.1 Tạo `app/i18n/types.ts` — define `Messages` type interface chung cho tất cả locales (home, browse, myList, common, admin sections)
- [x] 2.2 Tạo `app/i18n/vi.ts` — gom tất cả Vietnamese strings từ index.vue, browse.vue, my-list.vue, assistant.vue; export `vi` satisfies `Messages`
- [x] 2.3 Tạo `app/i18n/en.ts` — gom tất cả English strings tương ứng; export `en` satisfies `Messages`
- [x] 2.4 Tạo `app/composables/useLocale.ts` — shared composable: reactive `locale` (cookie-backed), computed `messages`, `setLocale()` function

## 3. Service Layer

- [x] 3.1 Tạo `app/services/catalog.service.ts` — `fetchTitles()`, `fetchTitleBySlug()`, `fetchGenres()`, `fetchPlayback()`
- [x] 3.2 Tạo `app/services/discovery.service.ts` — `fetchHome()`, `fetchTopTitles()`, `fetchPersonalized()`
- [x] 3.3 Tạo `app/services/auth.service.ts` — `fetchMe()`, `logout()`
- [x] 3.4 Tạo `app/services/search.service.ts` — `searchTitles()`
- [x] 3.5 Tạo `app/services/library.service.ts` — `fetchLibrary()`
- [x] 3.6 Tạo `app/services/admin.service.ts` — `fetchAdminTitles()`, `fetchAdminTitle()`, `updateTitle()`, `deleteTitle()`, `toggleFeatured()`, `fetchAdminGenres()`, `fetchAdminUsers()`, `updateUserRole()`, `fetchAdminReviews()`, `deleteReview()`, `fetchAdminOverview()`

## 4. Feature Composables — Public Pages

- [x] 4.1 Tạo `app/composables/useHomePage.ts` — extract từ index.vue: data fetching (home, top, personalized), derived computed (recommendedTitles, newReleaseTitles, titles2026, moviePicks, seriesPicks, continueWatching), locale switching, helper functions (formatViews, progressPercent, takeUniqueTitles)
- [x] 4.2 Refactor `app/pages/index.vue` — slim down: import useHomePage + useLocale, chỉ giữ template + event binding. Target: ≤ 150 dòng
- [x] 4.3 Tạo `app/composables/useBrowse.ts` — extract từ browse.vue: catalog/search loading, genre filter state, sort state, visible titles computed, debounced search
- [x] 4.4 Refactor `app/pages/browse.vue` — slim down: import useBrowse + useLocale. Target: ≤ 120 dòng
- [x] 4.5 Tạo `app/composables/useMyList.ts` — extract từ my-list.vue: library loading, tab state, progress helper, error handling
- [x] 4.6 Refactor `app/pages/my-list.vue` — slim down: import useMyList + useLocale. Target: ≤ 100 dòng
- [x] 4.7 Refactor `app/composables/useAuthSession.ts` — update import paths sang `types/auth.ts`; wire service layer (auth.service) thay vì raw `$api`

## 5. Feature Composables — Admin Pages

- [x] 5.1 Tạo `app/composables/useAdminOverview.ts` — extract từ admin/index.vue
- [x] 5.2 Refactor `app/pages/admin/index.vue` — slim down
- [x] 5.3 Tạo `app/composables/useAdminTitles.ts` — extract từ admin/titles.vue: CRUD operations, search/filter state, editor state, delete confirmation, pagination
- [x] 5.4 Refactor `app/pages/admin/titles.vue` — slim down. Target: ≤ 150 dòng (template-heavy do form)
- [x] 5.5 Tạo `app/composables/useAdminUsers.ts` — extract từ admin/users.vue
- [x] 5.6 Refactor `app/pages/admin/users.vue` — slim down
- [x] 5.7 Tạo `app/composables/useAdminReviews.ts` — extract từ admin/reviews.vue
- [x] 5.8 Refactor `app/pages/admin/reviews.vue` — slim down
- [x] 5.9 Tạo `app/composables/useAdminGenres.ts` — extract từ admin/genres.vue
- [x] 5.10 Refactor `app/pages/admin/genres.vue` — slim down

## 6. Remaining Pages & Cleanup

- [x] 6.1 Review và refactor `app/pages/login.vue` — extract inline logic nếu > 100 dòng script
- [x] 6.2 Review và refactor `app/pages/signup.vue` — extract inline logic nếu > 100 dòng script
- [x] 6.3 Review và refactor `app/pages/profile.vue` — extract inline logic nếu > 100 dòng script
- [x] 6.4 Review `app/pages/assistant.vue` — extract nếu cần
- [x] 6.5 Xóa tất cả inline type declarations trùng lặp trong pages (search: `type Title =` in pages/)
- [x] 6.6 Xóa tất cả inline i18n objects trong pages (search: `const text = computed` / `const copy = computed`)
- [x] 6.7 Verify imports — đảm bảo không còn unused imports sau refactor

## 7. Verification

- [x] 7.1 Chạy `bun run typecheck` — đảm bảo zero TS errors
- [x] 7.2 Chạy `bun run lint` — đảm bảo zero lint errors
- [x] 7.3 Chạy `bun test` — đảm bảo existing tests pass
- [x] 7.4 Chạy `bun run build` — đảm bảo production build thành công
- [x] 7.5 Test thủ công: Homepage (hero, trending, sections, locale switch)
- [x] 7.6 Test thủ công: Browse (search, filter, sort)
- [x] 7.7 Test thủ công: Admin (titles CRUD, users, reviews, genres)
- [x] 7.8 Verify mỗi page ≤ 150 dòng (trừ pages có form dài như admin/titles)
