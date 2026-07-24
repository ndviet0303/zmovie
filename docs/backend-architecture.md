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
local TF-IDF content model. For the demo, the deployed frontend sends that context
to the user's local AI service at the Mac's LAN address on port `8788`; the user's browser, not the
deployed backend, owns the localhost connection. The generator cannot create
suggestion IDs, so its text never controls which titles appear in the response.

For local development, install Ollama and pull the small Qwen model:

```bash
ollama pull qwen3:0.6b
```

Development settings enable the local AI service at `http://127.0.0.1:8788`.
Production keeps it disabled by default; configure `LocalAi__Enabled` and
`LocalAi__BaseUrl` through the secret/configuration system when a private model
service is available. The deterministic reply remains the fallback when the model
service is disabled or unavailable.

## Authentication direction

Google OAuth 2.0 / OpenID Connect is the selected identity provider. Authentication has not yet been wired into the API or Nuxt BFF. The future implementation will validate Google ID tokens using Google's issuer, JWKS, and the configured `GOOGLE_CLIENT_ID`; it will provision local users by the stable `sub` claim. ZMovie roles will be stored and managed locally, never inferred from a Google email address.

Required server-side configuration for that work:

```text
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
GOOGLE_REDIRECT_URI
GOOGLE_POST_LOGOUT_REDIRECT_URI
```

No client secret or Google token may be exposed to browser code, source control, or logs.

## Planned modules

Identity, Media, Playback sessions, Engagement, admin CMS, transactional outbox/worker, Redis, object storage, and operational audit endpoints are planned but are not implemented yet. New modules should preserve the existing dependency direction and be added as complete vertical slices, including validation and tests.
