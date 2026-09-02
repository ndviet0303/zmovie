# Frontend Architecture & Coding Guidelines

This directory contains the Nuxt 4 frontend of ZMovie. All frontend code must strictly comply with Clean Architecture standards.

See full rules specification in [`.agents/rules/frontend-architecture.md`](../.agents/rules/frontend-architecture.md).

---

## Quick Reference Rules

1. **Strict 3-Layer Separation**:
   - `Presentation` (`app/pages/`, `app/components/`): Thin UI views (script ≤ 30-50 lines).
   - `Application` (`app/composables/`): Use cases, reactive states, orchestration.
   - `Infrastructure & Services` (`app/services/`): Pure API transport functions (`ApiFetch`).
   - `Domain Types` (`app/types/`): Pure TypeScript interfaces & data contracts.

2. **Absolute Restrictions**:
   - **NEVER** call `$api`, `$fetch`, or native `fetch()` inside Vue components or pages. All data calls belong in `app/services/*`.
   - **NEVER** declare domain types or API responses inline (`type Title = ...`, etc.). Import from `~/types/*`.
   - **NEVER** declare hardcoded inline translation objects (`const copy = computed(...)`). Use centralized `useLocale()` and `~/i18n/*`.

3. **Service Layer Pattern**:
   - Functions accept optional `api?: ApiFetch`.
   - Fallback helper: `resolveApi(api) => api ?? (useNuxtApp().$api as ApiFetch)`.
   - Void endpoints return `.then(() => undefined)`.

4. **Testing & Quality Gates**:
   - `bun test`: Fast unit tests in `tests/` using `createMockApi`. Zero `any`.
   - `bun run lint`: Zero ESLint errors or warnings.
   - `bun run typecheck`: Zero TypeScript errors via `vue-tsc --noEmit`.
   - `bun run build`: Clean production build.
