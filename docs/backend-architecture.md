# Backend architecture

ZMovie is a .NET 10 modular-monolith foundation with four layers:

- `ZMovie.Api` hosts Minimal API endpoints, OpenAPI/Scalar, CORS, Problem Details, health endpoints, and composition.
- `ZMovie.Application` owns MediatR queries, FluentValidation, ErrorOr contracts, and application interfaces.
- `ZMovie.Domain` owns the Catalog title and episode entities.
- `ZMovie.Infrastructure` owns EF Core/PostgreSQL persistence, Meilisearch access, and catalog seed data.

## Implemented vertical slice

The implemented module is Catalog. It uses the `catalog` PostgreSQL schema and exposes:

- `GET /v1/catalog/titles`
- `GET /v1/catalog/titles/{slug}`
- `GET /v1/catalog/genres`
- `GET /v1/catalog/titles/{slug}/playback`
- `GET /v1/discovery/home`
- `GET /v1/search`
- `GET /health/live` and `GET /health/ready`

Catalog data supports Vietnamese and English title/synopsis fields. Search queries Meilisearch when configured and fall back to PostgreSQL when that dependency is unavailable. The API uses snake_case database naming, UUIDv7 entity identifiers, and `UpdatedAt` optimistic concurrency metadata.

`ZMovie.Application` does not depend on `ZMovie.Infrastructure`; the Infrastructure layer implements `ICatalogReadStore` and `ISearchCatalogStore` for the application layer.

## Current runtime

`compose.yaml` currently starts PostgreSQL 17 only. The Aspire AppHost currently starts the API project only. Database migration and seed calls in `Program.cs` remain intentionally disabled, so they must be run explicitly before a fresh environment serves catalog data.

## Production secrets (Infisical)

When `ASPNETCORE_ENVIRONMENT=Production`, the API fails fast unless it can retrieve
its configuration from [Infisical's .NET SDK](https://infisical.com/docs/sdks/languages/dotnet).
Create a dedicated **Universal Auth Machine Identity** with read-only access to the
production environment and inject only these bootstrap values through your runtime's
secret mechanism (for example, the hosting platform's encrypted environment variables):

```text
INFISICAL_CLIENT_ID
INFISICAL_CLIENT_SECRET
INFISICAL_PROJECT_ID
INFISICAL_ENVIRONMENT=prod       # optional; this is the default
INFISICAL_SECRET_PATH=/          # optional; this is the default
INFISICAL_API_URL=...            # optional; omit for https://app.infisical.com
```

Store application configuration in Infisical using .NET environment-style keys.
Double underscores are converted to configuration nesting, so the current API uses:

```text
ConnectionStrings__ZMovie
FrontendOrigin
Google__ClientId
Meilisearch__Url                # optional
Meilisearch__ApiKey             # optional
```

The API retrieves the values before dependency injection is configured and never logs
secret values. Do not put the machine identity client secret, database connection
string, or any application secret in `appsettings*.json`, container images, source
control, or CI logs. Scope the machine identity to the exact project, environment,
and secret path; rotate its client secret through Infisical when required.

## Personalized movie RAG

`POST /v1/assistant/context`, `POST /v1/assistant/chat`, and `GET /v1/discovery/for-you`
require an authenticated session. The assistant retriever combines the request with
the user's saved titles and watch history, then ranks catalog candidates with the
local TF-IDF content model. For the demo, the deployed backend sends that context
to the local AI service at the Mac's address on port `8788`. The generator cannot
create suggestion IDs, so its text never controls which titles appear in the response.

For local development, install Ollama and pull the small Qwen model:

```bash
ollama pull qwen3:0.6b
```

Development settings enable the local AI service at `http://127.0.0.1:8788`.
Production keeps it disabled by default; configure `LocalAi__Enabled` and
`LocalAi__BaseUrl` through the secret/configuration system when a private model
service is available. The deterministic reply remains the fallback when the model
service is disabled or unavailable.

## Authentication

Google Identity Services is the identity provider. The browser obtains a Google ID
token client-side and posts it to `POST /v1/auth/google`; `GoogleIdentityVerifier`
validates it against Google's issuer and JWKS with the configured `Google:ClientId`
as the audience, and users are provisioned by the stable `sub` claim. The API then
issues its own Data-Protection-encrypted cookie (`zmovie.session`) carrying the
user id, email, display name, avatar and role. Only `Google__ClientId` is required
server-side — there is no authorization-code flow, so no client secret, redirect
URI, or Google token ever reaches the server config or the browser bundle.

## Admin area

Roles live on `users.role` (`member` | `admin`, see `ZMovieRoles`) and are managed
locally — never inferred from an email domain. The role is written into the auth
cookie as a `ClaimTypes.Role` claim, and the whole `/v1/admin` group requires the
`ZMovie.Admin` policy (`RequireRole("admin")`).

Because the role is carried in the cookie, **a role change only takes effect on the
user's next sign-in.**

### Bootstrapping the first admin

Configure an allowlist of verified Google emails:

```text
Admin__Emails__0=owner@example.com
Admin__Emails__1=ops@example.com
```

A user whose verified email matches is promoted to admin on every sign-in. The
allowlist only ever **promotes**: removing an entry does not demote anyone, and a
role granted through the admin UI survives later sign-ins. Revoke through the UI.

Two guardrails prevent lockout: an admin cannot remove their own role, and the last
remaining admin cannot be demoted.

### Endpoints

All of these require the admin policy:

- `GET /v1/admin/overview` — catalog, engagement and identity counters
- `GET|PUT|DELETE /v1/admin/titles[/{slug}]`, `PATCH /v1/admin/titles/{slug}/featured`
- `GET /v1/admin/users`, `PATCH /v1/admin/users/{id}/role`
- `GET /v1/admin/reviews`, `DELETE /v1/admin/reviews/{id}`
- `GET|POST /v1/admin/genres`, `PUT|DELETE /v1/admin/genres/{id}`

Deleting a title also removes its episodes, saved entries, watch history, reviews,
view events and assistant learning events — the engagement tables carry no foreign
keys (migration `202607230003` dropped them), so the cascade is done in code, in
bounded batches, inside one transaction.

Renaming a genre rewrites that name on every title carrying it: `titles.genre`
stores a comma-joined list of display names rather than a foreign key, so the
rename would otherwise orphan every affected title. For the same reason the genre
filter and the per-genre title counts match by list membership, not equality.

## Catalog import

There is no in-app crawler. The catalog is populated by running the API as a CLI:

```bash
dotnet run --project src/ZMovie.Api -- --import-ophim-genres
dotnet run --project src/ZMovie.Api -- --import-ophim-catalog --max-pages 10 --with-episodes
```

Both branches migrate the database first and exit without serving.

### Frontend

`/admin/**` is `ssr: false, prerender: false`, so the session-gated area is never
baked into the public static output; it is reached through the SPA fallback in
`frontend/public/_redirects` and is disallowed in `robots.txt`. The `admin` route
middleware resolves the shared session (`useAuthSession`) and redirects anonymous
visitors to `/login?redirect=…`, or raises a 403 for signed-in non-admins. That
check is a UX affordance only — the API enforces authorization independently.

## Planned modules

Identity, Media, Playback sessions, Engagement, admin CMS, transactional outbox/worker, Redis, object storage, and operational audit endpoints are planned but are not implemented yet. New modules should preserve the existing dependency direction and be added as complete vertical slices, including validation and tests.
