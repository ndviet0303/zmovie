# Frontend Architecture & Clean Architecture Rules

ZMovie frontend is a Nuxt 4 (Vue 3, TypeScript, Tailwind CSS, Bun) application adhering to strict 3-layer Clean Architecture principles.

---

## 1. Layer Dependency Rules

Unidirectional dependency flow must be preserved at all times:
```text
Presentation Layer (Pages, Components)
              │
              ▼
Application Layer (Feature Composables, Locale)
              │
              ▼
Domain & Infrastructure Layer (Pure Types, API Services)
```

- **`Presentation Layer` (`app/pages/`, `app/components/`)**:
  - **Thin presentation only**: Responsible for layout, template binding, UI events, and view states.
  - Target script size: ≤ 30-50 lines of code.
  - **STRICTLY FORBIDDEN**: Never call `$api`, `$fetch`, or native `fetch()` directly in pages or components. All data fetching and mutations MUST go through Feature Composables or Domain Services.
  - **NO INLINE DOMAIN TYPES**: Do not define `type Title = ...`, `type Review = ...`, or API responses inline. Always import from `~/types/*`.
  - **NO INLINE I18N OBJECTS**: Do not define inline translation objects (`const copy = computed(...)`). Use centralized `useLocale()` and `~/i18n/*`.

- **`Application Layer` (`app/composables/`)**:
  - Encapsulates use-case business logic, reactive state (loading, errors, pagination, debounced filtering, modals), and UI workflow orchestration.
  - Calls Domain Services in `~/services/*` to retrieve or mutate backend data.
  - Composables are auto-imported by Nuxt from `app/composables/`.
  - Feature composables naming: `use<Feature>.ts` (e.g. `useHomePage`, `useBrowse`, `useMovieDetail`, `useWatchPlayer`, `useAssistant`, `useAdminTitles`).

- **`Domain Types Layer` (`app/types/`)**:
  - Pure TypeScript type definitions representing domain models, values, and API contracts.
  - Zero runtime dependencies; completely decoupled from Vue/Nuxt and HTTP transport.
  - Canonical modules:
    - `catalog.ts`: `TitleSummary`, `TitleDetail`, `TitleListResponse`, `PlaybackResponse`, `PlaybackEpisode`.
    - `discovery.ts`: `HomeResponse`, `TopTitle`, `TopPeriod`, `ContinueWatching`, `PersonalizedDiscovery`.
    - `library.ts`: `Library`, `LibraryTitle`, `HistoryItem`, `LibraryTab`.
    - `auth.ts`: `SessionUser`, `UserRole`.
    - `review.ts`: `Review`, `ReviewsResponse`, `CreateReviewPayload`.
    - `assistant.ts`: `AssistantReply`, `AssistantMessage`, `AssistantFeedbackPayload`.
    - `watch.ts`: `ViewRecordedResponse`, `LocalWatchProgress`.
    - `api-fetch.ts`: `ApiFetch` abstraction.

- **`Infrastructure & Services Layer` (`app/services/`)**:
  - Plain, pure TypeScript functions encapsulating HTTP endpoint URLs, HTTP methods, credentials, query parameters, and payload serialization.
  - **Dependency Injection**: Every service function accepts an optional `api?: ApiFetch`.
    - If provided, it uses the injected client (essential for fast, isolated unit tests).
    - If omitted, fallback to `resolveApi(api)`: `api ?? (useNuxtApp().$api as ApiFetch)`.
  - Void responses must return `.then(() => undefined)` to comply with `@typescript-eslint/no-invalid-void-type`.
  - Canonical service modules: `catalog.service.ts`, `discovery.service.ts`, `auth.service.ts`, `library.service.ts`, `assistant.service.ts`, `search.service.ts`, `admin.service.ts`.

---

## 2. Localization (i18n) Rules

- Single source of truth contract: `Messages` interface in `app/i18n/types.ts`.
- All supported language dictionaries (`vi.ts`, `en.ts`) must strictly satisfy the `Messages` type.
- Shared reactive state and locale switching is managed exclusively by `useLocale()` with cookie persistence (`zmovie-locale`).
- Structural key parity between languages is verified via automated tests (`tests/i18n.test.ts`).

---

## 3. Testing & Verification Standards

All changes to the frontend must pass the following quality gates before merging:

1. **Unit Tests (`bun test`)**:
   - Tests live in `tests/*.service.test.ts` and `tests/*.test.ts`.
   - Mock API interactions using `createMockApi` in `tests/test-utils.ts`.
   - Zero `any` types in test files (`@typescript-eslint/no-explicit-any` enforced).
2. **Linter (`bun run lint`)**:
   - ESLint must pass with 0 errors and 0 warnings.
   - Unused variables and invalid void types are strictly checked.
3. **Type Checking (`bun run typecheck`)**:
   - `nuxt prepare && vue-tsc --noEmit` must pass with 0 errors.
4. **Production Build (`bun run build`)**:
   - Nuxt client and Nitro server build must complete successfully with code 0.
