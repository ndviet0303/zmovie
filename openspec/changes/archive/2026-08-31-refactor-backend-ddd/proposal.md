## Why

The backend has the compile-time shape of Clean Architecture, but its domain objects are mutable persistence records and important policies live inside EF Core stores. As Catalog, Identity, Engagement, Analytics, and Personalization grow, the shared `CatalogDbContext` and cross-module stores make changes increasingly risky and allow business invariants to vary by entry point.

## What Changes

- Refactor the backend into an explicitly bounded modular monolith with Catalog, Identity & Access, Engagement, Analytics, and Personalization ownership boundaries.
- Encapsulate write-side domain state behind aggregate behavior, focused value objects, domain policies, and module-owned repositories.
- Keep query-side projections direct and efficient; DDD aggregates will not be forced onto read-only catalog, search, discovery, or administration dashboards.
- Replace cross-domain god stores, especially `IAdminStore`, with module-owned commands, query services, and explicit application orchestration.
- Split persistence ownership into module DbContexts and configurations while continuing to use the existing PostgreSQL deployment.
- Normalize the Catalog title/genre relationship and migrate existing comma-separated genre data without changing response contracts.
- Introduce architecture, domain-invariant, migration, PostgreSQL integration, and HTTP contract tests before moving each write slice.
- Preserve existing routes, request and response shapes, authentication scheme, and user-visible behavior throughout the refactor.

### Non-goals

- No microservice extraction, distributed transactions, or new deployment units.
- No frontend redesign or API version change.
- No recommendation-model replacement or unrelated feature work.
- No change from the current externally visible hard-delete semantics; alternative retention or retirement behavior requires a separate change.

## Capabilities

### New Capabilities

None. This change restructures the implementation without introducing externally observable capabilities.

### Modified Capabilities

None. Existing API and product requirements remain unchanged, so this change opts out of delta specs.

## Impact

- Affected code: all backend projects under `backend/src`, backend tests, EF Core mappings and migrations, and backend architecture documentation.
- API impact: no intentional route, payload, status-code, cookie, or authorization-policy changes.
- Data impact: in-place PostgreSQL migrations are required for module ownership and normalized title/genre associations; migrations must preserve existing data and support a controlled rollback.
- Dependency impact: architecture and PostgreSQL integration test tooling may be added; unused infrastructure packages may be removed after usage is verified.
- Operational impact: the API remains a single deployable modular monolith and continues to use the existing Aspire/AppHost topology.
