# Backend DDD refactor baseline

This baseline was captured before moving any production type for OpenSpec change
`refactor-backend-ddd`.

## Source and toolchain

- Captured: 2026-08-31
- Git commit: `2fb266cfdea82e9eb80374c59f1233d8084a0ab8`
- SDK: .NET SDK `10.0.400`
- Solution: `backend/ZMovie.slnx`
- Target framework: `net10.0`

## Test baseline

Command, run from `backend/`:

```bash
dotnet test ZMovie.slnx --no-restore --nologo
```

Result:

```text
Passed: 58
Failed: 0
Skipped: 0
Total: 58
```

The baseline contains one test assembly, `ZMovie.Api.Tests`. It builds all six
production projects before running the tests. At capture time these tests are
primarily in-process unit/application/infrastructure tests; representative HTTP
and PostgreSQL migration coverage is added by the subsequent safety-net tasks.

## HTTP route baseline

There are 40 Minimal API operations. `Authenticated` means the default cookie
scheme is required. `Admin` means the `ZMovie.Admin` policy is required. All
other operations are anonymous unless authentication is applied globally later.

| Surface | Method and route | Authorization |
| --- | --- | --- |
| Health | `GET /health/live` | Anonymous |
| Health | `GET /health/ready` | Anonymous |
| Catalog | `GET /v1/catalog/titles` | Anonymous |
| Catalog | `GET /v1/catalog/titles/{slug}` | Anonymous |
| Catalog | `GET /v1/catalog/genres` | Anonymous |
| Catalog | `GET /v1/catalog/titles/{slug}/playback` | Anonymous |
| Catalog | `POST /v1/catalog/titles/{slug}/views` | Anonymous |
| Catalog | `GET /v1/catalog/titles/{slug}/reviews` | Anonymous |
| Discovery | `GET /v1/discovery/home` | Anonymous |
| Discovery | `GET /v1/discovery/top/{period}` | Anonymous |
| Discovery | `GET /v1/discovery/for-you` | Authenticated |
| Search | `GET /v1/search` | Anonymous |
| Auth | `POST /v1/auth/google` | Anonymous |
| Auth | `GET /v1/auth/me` | Authenticated |
| Auth | `POST /v1/auth/logout` | Authenticated |
| Engagement | `GET /v1/me/library` | Authenticated |
| Engagement | `PUT /v1/me/saved/{slug}` | Authenticated |
| Engagement | `DELETE /v1/me/saved/{slug}` | Authenticated |
| Engagement | `POST /v1/me/history/{slug}` | Authenticated |
| Engagement | `PUT /v1/me/titles/{slug}/review` | Authenticated |
| Engagement | `DELETE /v1/me/titles/{slug}/review` | Authenticated |
| Assistant | `POST /v1/assistant/context` | Authenticated |
| Assistant | `GET /v1/assistant/context` | Authenticated |
| Assistant | `POST /v1/assistant/chat` | Authenticated |
| Assistant | `GET /v1/assistant/chat` | Authenticated |
| Assistant | `POST /v1/assistant/feedback` | Authenticated |
| Admin | `GET /v1/admin/overview` | Admin |
| Admin | `GET /v1/admin/titles` | Admin |
| Admin | `GET /v1/admin/titles/{slug}` | Admin |
| Admin | `PUT /v1/admin/titles/{slug}` | Admin |
| Admin | `PATCH /v1/admin/titles/{slug}/featured` | Admin |
| Admin | `DELETE /v1/admin/titles/{slug}` | Admin |
| Admin | `GET /v1/admin/users` | Admin |
| Admin | `PATCH /v1/admin/users/{id:guid}/role` | Admin |
| Admin | `GET /v1/admin/reviews` | Admin |
| Admin | `DELETE /v1/admin/reviews/{id:guid}` | Admin |
| Admin | `GET /v1/admin/genres` | Admin |
| Admin | `POST /v1/admin/genres` | Admin |
| Admin | `PUT /v1/admin/genres/{id:guid}` | Admin |
| Admin | `DELETE /v1/admin/genres/{id:guid}` | Admin |

Cookie behavior at capture time:

- Authentication cookie: `zmovie.session`, HTTP-only, `SameSite=Lax` in
  Development and `SameSite=None` otherwise, secure according to environment.
- Anonymous view session cookie: `zmovie.analytics-session`, HTTP-only,
  `SameSite=Lax`, secure on HTTPS, and valid for 30 days.

## Migration baseline

`ZMovie.Infrastructure.Persistence.CatalogDbContext` currently maps all Catalog,
Identity, Engagement, Analytics, and Assistant learning tables in the `public`
schema. The immutable legacy chain contains these 17 migrations, in order:

1. `202607210001_InitialCatalog`
2. `202607210002_AddEpisodes`
3. `202607220001_AddTitleViewEvents`
4. `202607230001_AddGoogleUsers`
5. `202607230002_AddUserLibrary`
6. `202607230003_MoveUserLibraryToEngagement`
7. `202607230004_AddPlayableProgress`
8. `202607230005_MoveTitleViewEventsToEngagement`
9. `202607230006_AddTitleReviews`
10. `202607230007_AddCatalogGenres`
11. `202607240001_MoveCatalogTablesToPublicSchema`
12. `202607240002_MoveEngagementTablesToPublicSchema`
13. `202607240003_DropLegacySchemas`
14. `202607260001_AddAssistantLearningEvents`
15. `202607260002_AddUserRoles`
16. `202607260003_AddTitleIdIndexes`
17. `202607270001_AddTitleUpdatedAtIndex`

The refactor must not edit or reorder this chain. Module migration histories and
additive schema changes start after this baseline and are verified against both a
fresh database and a database upgraded to this legacy head.
