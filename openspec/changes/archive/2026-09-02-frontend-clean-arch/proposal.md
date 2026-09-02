## Why

Frontend ZMovie hiện tại đang mắc hội chứng "god component" — các page lớn (index.vue 548 dòng, browse.vue 427 dòng, admin/titles.vue 536 dòng) trộn lẫn API calls, business logic, i18n strings, type definitions, và template trong cùng một file. Điều này khiến việc mở rộng tính năng, test, và onboard developer mới trở nên khó khăn. Refactor sang clean architecture sẽ tạo nền tảng bền vững cho các tính năng sắp tới (review system, payment, notifications).

## What Changes

- **Extract Service Layer**: Tạo `services/` directory chứa các thin wrappers quanh `$api` per domain (catalog, discovery, auth, library, admin). Mỗi service là single source of truth cho API calls của domain đó.
- **Extract Feature Composables**: Tách business logic từ pages vào composables theo feature (useHomePage, useBrowse, useMyList, useAdminTitles, ...). Pages chỉ còn thin orchestration + template.
- **Consolidate Types**: Loại bỏ inline type declarations trùng lặp (Title type lặp ≥3 lần). Tạo domain type files re-export từ auto-generated `api.d.ts` và extend khi cần.
- **Centralize i18n**: Gom i18n strings đang inline trong mỗi page vào `i18n/` directory theo locale, dùng composable `useLocale()` thống nhất.
- **Slim Down Pages**: Mỗi page giảm xuống ~50-150 dòng, chỉ chứa layout composition và event binding.

## Capabilities

### New Capabilities

_(none — pure refactor, no new externally observable behavior)_

### Modified Capabilities

_(none — skip_specs enabled; all changes are internal restructuring without altering API contracts, routing, or user-facing behavior)_

## Impact

- **Code**: Toàn bộ `frontend/app/` — pages, composables, types, và thêm mới services/, i18n/ directories
- **Files affected**: ~20 files refactor, ~15 files mới
- **APIs**: Không thay đổi — chỉ di chuyển API calls vào service layer
- **Dependencies**: Không thêm dependency mới (không dùng @nuxtjs/i18n — chỉ centralize strings thủ công)
- **Risk**: Medium — refactor rộng nhưng không thay đổi behavior; cần test từng page sau mỗi batch refactor
- **Breaking changes**: Không có
