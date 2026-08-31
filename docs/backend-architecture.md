# Backend Architecture

ZMovie is a .NET 10 modular-monolith adhering to strict Domain-Driven Design (DDD) principles and a clean four-layer architecture:

- `ZMovie.Domain`: Pure domain logic containing aggregates, entities, value objects, domain events, and domain rules. Zero third-party dependencies, zero framework dependencies, and zero system wall-clock access (`TimeProvider` / `DateTimeOffset` are passed explicitly).
- `ZMovie.Application`: Application use cases, MediatR commands/queries, FluentValidation validators, error contracts (`ErrorOr`), and domain ports/interfaces.
- `ZMovie.Infrastructure`: Persistence adapters (EF Core with PostgreSQL), isolated DbContexts per bounded context, Meilisearch client, external identity verifiers, AI text generators, caching, and background coordinators.
- `ZMovie.Api`: Minimal API endpoints, OpenAPI/Scalar, CORS, Problem Details, authentication cookies, claim translation adapters (`UserIdentityAdapter`), and ASP.NET Core composition root.

---

## Bounded Contexts & Context Map

The application is decomposed into isolated bounded contexts, each owning its domain models, application contracts, and persistence tables:

```text
+-------------------+      +-------------------+      +----------------------+
| Identity & Access |      |      Catalog      |<-----|  Engagement (Library |
| (Users, Roles)    |      | (Titles, Genres)  |      |  & Title Reviews)    |
+-------------------+      +-------------------+      +----------------------+
          ^                          ^                          ^
          |                          |                          |
          +--------------------------+--------------------------+
                                     |
               +---------------------+---------------------+
               |                     |                     |
               v                     v                     v
      +-----------------+   +-----------------+   +-----------------+
      |    Analytics    |   | Personalization |   |  Administration |
      | (Views/Ranks)   |   |   & Assistant   |   |   (Backoffice)  |
      +-----------------+   +-----------------+   +-----------------+
```

### 1. Catalog Context
- **Ownership**: Titles, episodes, genres, localized metadata (Vietnamese / English), runtime, and playback links.
- **Persistence**: `CatalogDbContext` owning `titles`, `episodes`, `genres`, and `title_genres`.
- **Migrations**: `__ef_migrations_history_catalog`.

### 2. Identity & Access Context
- **Ownership**: User aggregate, external identity (`sub`), role value object (`Role`), display info, and last-admin demotion protection policy.
- **Persistence**: `IdentityDbContext` owning `users`.
- **Migrations**: `__ef_migrations_history_identity`.

### 3. Engagement Context
- **Ownership**: User library (`saved_titles`), watch progress (`watch_history`), and user ratings/reviews (`title_reviews`).
- **Persistence**: `EngagementDbContext` owning `saved_titles`, `watch_history`, and `title_reviews`.
- **Migrations**: `__ef_migrations_history_engagement`.

### 4. Analytics Context
- **Ownership**: Title view facts (`title_view_events`), session deduplication, view counts, and time-windowed top-ranking aggregations.
- **Persistence**: `AnalyticsDbContext` owning `title_view_events`.
- **Migrations**: `__ef_migrations_history_analytics`.

### 5. Personalization & Assistant Context
- **Ownership**: Recommendation feedback, assistant learning impressions (`assistant_learning_events`), learned ranking weights, and TinyContent TF-IDF candidate ranking.
- **Persistence**: `PersonalizationDbContext` owning `assistant_learning_events`.
- **Migrations**: `__ef_migrations_history_personalization`.

### 6. Administration (Backoffice)
- **Role**: Non-owning orchestration layer for admin operations.
- **Reads**: `IAdminDashboardQueries` / `EfAdminDashboardQueries` compose read-only projections across contexts.
- **Writes**: Dispatched to context-owned services (`ICatalogAdministrationService`, `IUserRepository`).
- **Atomic Hard Deletions**: Coordinated via `IAdminTitleDeletionCoordinator` and `ITransactionCoordinator`, deleting records in strict dependent order: `Engagement` -> `Analytics` -> `Personalization` -> `Catalog`.

---

## Layer Rules & Dependency Direction

The solution maintains strict unidirectional project references:

```text
Domain  <---  Application  <---  Infrastructure  <---  API
```

1. **Domain Layer**:
   - Must NOT reference any other layer or third-party packages.
   - Domain contexts do NOT reference other domain contexts.
   - Value objects are immutable; aggregates encapsulate business invariants.
   - Time is never read from static clocks (`DateTime.Now` / `DateTime.UtcNow`). All time values enter as parameters.

2. **Application Layer**:
   - References `Domain` only.
   - Organizes features by context into Commands, Queries, Validators, and Ports.
   - Returns `ErrorOr<T>` for domain outcomes.

3. **Infrastructure Layer**:
   - References `Application` and `Domain`.
   - Implements ports using EF Core, PostgreSQL, Meilisearch, and HTTP clients.
   - Each bounded context has its own independent `DbContext`. DbSets and database configurations are never shared across contexts.

4. **API Layer**:
   - Acts as the composition root.
   - Translates HTTP requests, routes, cookies, and claims to Application commands/queries.
   - Consolidates claim extraction in `UserIdentityAdapter`.

---

## Multi-Context Transactions & PostgreSQL Ownership

When an administrative operation spans multiple contexts (e.g., deleting a title and its associated reviews, watch history, views, and learning events), atomicity is guaranteed without distributed transactions:

1. `ITransactionCoordinator` opens a single PostgreSQL transaction on the root connection (`CatalogDbContext`).
2. Participating contexts (`IdentityDbContext`, `EngagementDbContext`, `AnalyticsDbContext`, `PersonalizationDbContext`) enlist into the same connection and active `DbTransaction` via `UseTransaction(dbTx)`.
3. If any step fails, the entire transaction is rolled back cleanly.

---

## Module Registration & DI Structure

Service registration is partitioned into modular extension methods in `DependencyInjection.cs`:

- `services.AddCatalogModule(connectionString)`
- `services.AddIdentityModule(connectionString)`
- `services.AddEngagementModule(connectionString)`
- `services.AddAnalyticsModule(connectionString)`
- `services.AddPersonalizationModule(connectionString)`
- `services.AddAdministrationModule(configuration)`
- `services.AddAssistantModule(configuration)`
- `services.AddSearchModule()`

`AddZMovieInfrastructure` composes all module registrations during application startup.

---

## Migration & Deployment Procedure

1. **Fresh Database Deployment**:
   - Each module's `MigrateAsync()` runs during startup or via explicit migration jobs.
   - All migrations use idempotent `CREATE TABLE IF NOT EXISTS` and `CREATE INDEX IF NOT EXISTS` statements.
   - Dedicated history tables track each module independently.

2. **Legacy Head Upgrade**:
   - Pre-existing monolithic database schemas upgrade smoothly without table collision.

3. **Testing Suite**:
   - Unit & Layer Tests: In-memory validation of aggregates, validators, queries, and handlers.
   - PostgreSQL Integration Tests: Run against real PostgreSQL instances via Testcontainers, verifying schema creation, foreign keys, cascades, indexes, and cross-context deletion rollbacks.
   - HTTP Contract Tests: Verify route templates, status codes, cookies, and API contracts.
