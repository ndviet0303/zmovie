## Context

See [proposal.md](proposal.md) for motivation. Hiện tại `frontend/app/` theo cấu trúc flat Nuxt mặc định: pages chứa mọi thứ (types, API calls, business logic, i18n, template). Không có service layer, types trùng lặp giữa các files, và i18n strings hardcode inline.

Constraints:
- **Nuxt 4.5** auto-imports composables từ `composables/` và components từ `components/` — cần giữ convention này
- **`api.d.ts`** được generate tự động từ OpenAPI — không sửa, chỉ re-export
- **shadcn-vue** components nằm trong `components/ui/` — giữ nguyên
- Admin area là client-only (SSR off) — service layer phải hoạt động cả SSR lẫn CSR

## Goals / Non-Goals

**Goals:**
- Pages ≤ 150 dòng — chỉ chứa layout composition, event binding, và composable calls
- Single source of truth cho mỗi domain type (không duplicate)
- API calls tập trung tại service layer — dễ mock, dễ thay URL, dễ thêm interceptors
- i18n strings tập trung — dễ thêm ngôn ngữ mới, dễ tìm string
- Composables encapsulate state + derived data + side-effects per feature
- Giữ nguyên behavior hiện tại 100% — zero regressions

**Non-Goals:**
- Không thêm `@nuxtjs/i18n` hoặc bất kỳ dependency mới nào
- Không thay đổi routing structure
- Không refactor `components/ui/` (shadcn-vue)
- Không tạo Pinia stores (composables đủ cho scale hiện tại)
- Không viết unit tests trong change này (sẽ là change riêng)

## Decisions

### 1. Layer Architecture: Services → Composables → Pages

**Decision**: 3-layer architecture với dependency direction rõ ràng.

```
Pages → Composables → Services → $api (plugin)
  ↓          ↓            ↓
Types ←←←←←←←←←←←←←←←←←←┘
```

**Rationale**: Phù hợp với Vue/Nuxt ecosystem. Composables là idiomatic Vue pattern cho state management nhẹ. Service layer tách biệt infrastructure concern (HTTP calls) khỏi business logic.

**Alternatives considered**:
- **Pinia stores**: Overkill cho app size hiện tại. Composables với `useState()` của Nuxt đã đủ cho shared state (SSR-safe). Nếu scale lên sẽ migrate composables → Pinia dễ dàng.
- **Repository pattern (class-based)**: Quá nặng cho frontend. Plain functions + type safety đủ rồi.

### 2. Service Layer: Plain functions, không class

**Decision**: Mỗi service là một file export named functions. Inject `$api` qua `useNuxtApp()` bên trong function hoặc nhận qua parameter.

```typescript
// services/catalog.service.ts
export function fetchTitles(api: ApiFetch, params: { locale?: string }) {
  return api<TitleListResponse>('/v1/catalog/titles', { query: params })
}
```

**Rationale**:
- Tree-shakeable — chỉ import function cần dùng
- Dễ test — pass mock `api` function
- Consistent với Nuxt ecosystem (functional, không OOP)
- Không cần DI container

**Alternatives considered**:
- **Class-based services**: Cần instantiation, khó tree-shake, không idiomatic Nuxt
- **Composable-as-service** (useXxxApi): Gây confusion giữa data-fetching composables và state-management composables

### 3. Types: Re-export + Extend từ api.d.ts

**Decision**: Tạo domain type files (`types/catalog.ts`, `types/discovery.ts`, etc.) re-export types từ auto-generated `api.d.ts` và thêm frontend-only types.

```typescript
// types/catalog.ts
import type { components } from './api'
export type TitleSummary = components['schemas']['TitleSummary']
export type TitleDetail = components['schemas']['TitleDetail']
// Frontend-only types không có trong API
export type TitleFilter = { genre: string; type: string; sort: SortOrder }
export type SortOrder = 'latest' | 'oldest' | 'title'
```

**Rationale**: api.d.ts là source of truth từ backend. Re-export giữ sync tự động khi run `generate:api`. Frontend-only types (filter state, UI state) tách riêng nhưng cùng file domain.

**Alternatives considered**:
- **Copy types manually**: Đang làm vậy → root cause của duplication
- **Import trực tiếp `components['schemas']['X']`**: Verbose, khó đọc, cần wrapper anyway

### 4. i18n: Centralized TS objects, không dùng library

**Decision**: Tạo `i18n/vi.ts` và `i18n/en.ts` export flat objects. Composable `useLocale()` cung cấp reactive locale state + getter cho messages.

```typescript
// i18n/vi.ts
export const vi = {
  home: {
    newRelease: 'Mới mẻ',
    watchNow: 'Xem ngay',
    // ...
  },
  browse: { ... },
  common: { ... },
}
```

```typescript
// composables/useLocale.ts
export function useLocale() {
  const locale = useCookie<'vi' | 'en'>('zmovie-locale', { default: () => 'vi' })
  const messages = computed(() => locale.value === 'vi' ? vi : en)
  return { locale, messages, setLocale }
}
```

**Rationale**: Đơn giản, type-safe (autocomplete keys), zero dependency. App chỉ có 2 locales, không cần routing-based i18n hay lazy-loading. Khi cần scale lên nhiều locales, migrate sang `@nuxtjs/i18n` straightforward.

**Alternatives considered**:
- **@nuxtjs/i18n**: Full-featured nhưng overkill (locale routing, lazy loading, pluralization) — app chỉ cần simple key-value lookup
- **Giữ inline**: Root cause, proposal đã giải thích why not

### 5. Composable Organization: Per-feature, không per-layer

**Decision**: Tổ chức composables theo feature/page, không theo technical concern.

```
composables/
├── useLocale.ts          ← shared i18n state
├── useAuthSession.ts     ← existing, keep
├── useZMovieSeo.ts       ← existing, keep
├── useHomePage.ts         ← index.vue business logic
├── useBrowse.ts           ← browse.vue business logic
├── useMyList.ts           ← my-list.vue business logic
├── useAdminTitles.ts      ← admin/titles.vue business logic
├── useAdminOverview.ts    ← admin/index.vue business logic
├── useAdminUsers.ts       ← admin/users.vue
├── useAdminReviews.ts     ← admin/reviews.vue
└── useAdminGenres.ts      ← admin/genres.vue
```

**Rationale**: Mỗi composable map 1:1 với page, dễ tìm code. Feature composable orchestrate service calls, manage local state, compute derived data. Shared concerns (auth, locale, SEO) tách riêng.

**Alternatives considered**:
- **Nested folders** (`composables/catalog/`, `composables/admin/`): Thêm nesting không cần thiết cho ~12 files. Khi vượt 20+ composables, sẽ xem lại.
- **Single composable per domain**: Quá lớn — `useAdmin()` sẽ lại thành god composable

### 6. Service File Naming & Structure

**Decision**: `services/<domain>.service.ts` — mỗi file group API calls theo domain.

```
services/
├── catalog.service.ts     ← /v1/catalog/* endpoints
├── discovery.service.ts   ← /v1/discovery/* endpoints
├── auth.service.ts        ← /v1/auth/* endpoints
├── library.service.ts     ← /v1/me/library endpoint
├── search.service.ts      ← /v1/search endpoint
└── admin.service.ts       ← /v1/admin/* endpoints
```

**Rationale**: `.service.ts` suffix phân biệt rõ với composables và types. Domain grouping match với backend API structure (đã có convention `/v1/<domain>/`).

### 7. ApiFetch Type Abstraction

**Decision**: Tạo shared type `ApiFetch` cho $fetch instance, dùng xuyên suốt service layer.

```typescript
// types/api-fetch.ts
import type { $Fetch } from 'ofetch'
export type ApiFetch = $Fetch
```

**Rationale**: Tránh couple service layer với Nuxt plugin internals. Dễ mock trong tests. Mọi service nhận `ApiFetch` parameter thay vì gọi `useNuxtApp()` trực tiếp.

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| Refactor rộng gây regression | High | Refactor incremental per page, test thủ công sau mỗi batch. Giữ git commits granular |
| Nuxt auto-import conflict với flat composables | Low | Nuxt 4 auto-imports từ `composables/` — flat structure hoạt động tốt. Test ngay batch đầu |
| Service functions không auto-imported bởi Nuxt | Low | Services import explicitly — đây là design choice, không phải bug. Services là infrastructure, không cần auto-import |
| i18n structure thiếu type-safety cho missing keys | Medium | Dùng TypeScript satisfies + shared Messages type để đảm bảo vi và en cùng shape |
| Performance: thêm layer = thêm function calls | Negligible | Overhead không đáng kể — chỉ là function composition, không network roundtrip. V8 inline tốt |
