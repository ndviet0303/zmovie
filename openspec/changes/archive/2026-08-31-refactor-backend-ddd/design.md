## Context

See `proposal.md` for motivation and scope. The current solution already enforces the outer project-reference direction:

```text
ZMovie.Api -> ZMovie.Application -> ZMovie.Domain
          +-> ZMovie.Infrastructure -> ZMovie.Application + ZMovie.Domain
```

The problem is inside those projects. Application handlers mostly pass primitives and DTOs to infrastructure stores; infrastructure mutates public EF-shaped domain records. Catalog, Identity, Engagement, Analytics, and Assistant learning share `CatalogDbContext`, while `IAdminStore` performs queries and writes across all of them. Existing migrations also place every table in `public` and several cross-table foreign keys were removed.

The backend is approximately 4,000 lines of production C# and is deployed as one API. A project-per-layer-per-bounded-context layout would create disproportionate project and migration churn at this size. The design therefore strengthens domain and module boundaries inside the existing four-layer solution first, with automated architecture tests providing enforcement.

The refactor must preserve existing HTTP contracts and remain deployable throughout. Existing data cannot be discarded, and PostgreSQL-specific transaction behavior must be covered by integration tests rather than EF InMemory alone.

## Goals / Non-Goals

**Goals:**

- Give every write operation a clear owning bounded context and application use case.
- Enforce business invariants in domain behavior regardless of whether a write originates from HTTP, an importer, seed data, or an administration workflow.
- Separate aggregate repositories from query projections.
- Make cross-context dependencies explicit through contracts or an application coordinator.
- Split persistence mapping and migration ownership by module without splitting the deployed database.
- Preserve API, authentication, authorization, and externally visible deletion behavior.
- Keep the codebase continuously buildable and releasable during migration.

**Non-Goals:**

- Introduce a generic DDD framework, mandatory aggregate base class, event bus, or repository for every table.
- Move query-only behavior into aggregates.
- Split modules into services or independently deployed databases.
- Rewrite search, recommendation math, or the OPhim integration except where required to call domain behavior.
- Introduce eventual consistency for operations that are currently atomic.

## Decisions

### 1. Use five bounded contexts and two non-owning application surfaces

The target context map is:

```text
                          +------------------+
HTTP / Backoffice ------> | Application      |
                          | orchestration    |
                          +---------+--------+
                                    |
          +-------------------------+-------------------------+
          |                |                |                  |
          v                v                v                  v
      Catalog       Identity & Access   Engagement         Analytics
          |                |                |                  |
          +----------------+--------+-------+------------------+
                                    |
                                    v
                             Personalization
```

Ownership is defined as follows:

- **Catalog** owns titles, playables/episodes, genres, localization metadata, and the OPhim anti-corruption mapping.
- **Identity & Access** owns users, external identities, roles, sign-in profile updates, and the last-admin policy.
- **Engagement** owns saved titles, watch progress, and reviews. These remain small aggregate roots; there is no unbounded `UserLibrary` aggregate.
- **Analytics** owns append-only view facts, view deduplication, period calculation, and top-title counters.
- **Personalization** owns recommendation impressions, feedback, reward policy, and personalization profiles.
- **Search/Discovery** remain Catalog-oriented read capabilities, not independent bounded contexts.
- **Administration/Assistant** are application surfaces. Administration composes read models and dispatches writes to the owning context. Assistant orchestrates Catalog, Engagement, Personalization, and an external text generator without owning their data.

Alternative considered: treat every existing namespace as a bounded context. This was rejected because Administration and Search do not own independent business models, while Assistant learning belongs with Personalization.

### 2. Keep the existing layer projects and enforce module rules automatically

The existing `ZMovie.Domain`, `ZMovie.Application`, `ZMovie.Infrastructure`, and `ZMovie.Api` projects remain. Each is organized by the context map:

```text
ZMovie.Domain/
  Catalog/
  Identity/
  Engagement/
  Analytics/
  Personalization/

ZMovie.Application/
  Catalog/{Commands,Queries,Contracts}/
  Identity/{Commands,Queries,Contracts}/
  Engagement/{Commands,Queries,Contracts}/
  Analytics/{Commands,Queries,Contracts}/
  Personalization/{Commands,Queries,Contracts}/
  Backoffice/

ZMovie.Infrastructure/
  <Context>/{Persistence,Adapters}/
```

Architecture tests enforce:

- Domain has no framework or outer-layer dependency.
- A context's Domain namespace does not reference another context's Domain namespace.
- Application does not reference Infrastructure or API.
- Cross-context application calls use explicitly named contract types, never another context's aggregate.
- Infrastructure adapters implement inward-facing application ports.
- API contains transport mapping and composition only.

Alternative considered: create fifteen projects for five contexts times three layers. This gives stronger compiler enforcement but is excessive for the current codebase and makes the first refactor a solution-layout rewrite. Assembly extraction remains possible after the module boundaries stabilize.

### 3. Apply tactical DDD only to the write side

Commands load aggregate state through a module-owned repository, invoke domain behavior, and commit through the module unit of work. Queries continue to project DTOs directly with `AsNoTracking`, including Catalog browse/search, discovery, administration dashboards, and reporting.

Application validators own transport/use-case shape constraints such as required fields and maximum request lengths. Domain objects independently own rules that must hold for every write path. Expected domain rejections use context-specific decision/result types and are mapped by Application to the existing `ErrorOr` codes. Domain remains independent of MediatR, FluentValidation, ErrorOr, EF Core, and ASP.NET Core.

Initial aggregate and value-object boundaries are:

- `Title`: metadata, title type, release year, runtime, featured state, localized text, and genre assignments.
- `Episode`: a separate aggregate root linked by `TitleId`; import/upsert does not require loading every episode into `Title`.
- `Genre`: controlled taxonomy identity and name.
- `User`: profile, external identities, and role transitions.
- `SavedTitle`, `WatchProgress`, and `Review`: separate Engagement roots keyed by typed user/title/playable identities.
- `ViewFact`: append-only Analytics record created only after the deduplication policy accepts a view.
- `Recommendation`, `Impression`, and `Feedback`: Personalization concepts with validated event type and reward policy.

Focused value objects include typed IDs, `TitleSlug`, `TitleType`, `Rating`, `WatchPosition`, `Role`, and `ExternalIdentity`. HTTP/application contract DTOs continue to expose strings and GUIDs and translate at the boundary. Generic wrappers for every string or number are explicitly avoided.

Domain methods receive time as an argument when time affects a decision. Application obtains time from `TimeProvider`; entities no longer call `DateTimeOffset.UtcNow` while applying business behavior.

### 4. Separate repositories, queries, and external adapters

The current `*Store` interfaces mix several responsibilities. They are replaced incrementally:

- Aggregate repositories return domain aggregates and expose only operations needed by commands.
- Query interfaces return application read models and may use optimized SQL/EF projections.
- External integrations, such as Google identity verification, OPhim, local AI, and the recommendation model, remain adapters behind application ports.
- Pure model mathematics may stay in Infrastructure; business choices such as allowed feedback events, reward semantics, exclusion rules, and deduplication windows belong to Domain/Application.

`IAdminStore` is removed. `IAdminDashboardQueries` may intentionally compose cross-module read data, but every administration write sends the same owner-module command used by non-admin entry points.

Alternative considered: create one repository per EF entity. This was rejected because it merely renames DbSets and adds indirection without expressing aggregate or use-case boundaries.

### 5. Use explicit synchronous coordination for atomic cross-context operations

The API currently hard-deletes a title and its dependent records atomically. To preserve that behavior, a Backoffice application coordinator invokes context-owned cleanup ports inside one PostgreSQL transaction:

```text
DeleteTitle
  -> Catalog verifies target
  -> Engagement removes saved/progress/review data
  -> Analytics removes view data
  -> Personalization removes learning data
  -> Catalog removes episodes, assignments, and title
  -> commit
```

Each module owns the SQL for its tables; the coordinator owns ordering and transaction scope. No module reaches into another module's DbSet. A later change may replace hard deletion with retirement and integration events, but this change does not introduce eventual consistency or an outbox.

The last-admin operation follows the same split: Identity Domain owns the decision, while Infrastructure owns the serializable PostgreSQL transaction needed to prevent concurrent demotions.

### 6. Split EF Core ownership while retaining one PostgreSQL database

Infrastructure introduces module DbContexts:

- `CatalogDbContext`
- `IdentityDbContext`
- `EngagementDbContext`
- `AnalyticsDbContext`
- `PersonalizationDbContext`

The current all-table context is renamed to `LegacyCatalogDbContext` during cutover so the target `CatalogDbContext` name denotes Catalog ownership only. The module contexts share the configured Npgsql data source and database but map only module-owned tables. Cross-context references are scalar typed IDs with EF conversions; navigation properties never cross a context boundary.

After existing orphan rows are audited and repaired, database foreign keys enforce durable user, title, and playable references. The referencing module owns each constraint migration, while the domain and EF models continue to expose only scalar IDs rather than cross-context navigations.

The existing migration chain remains immutable. Each new context receives a no-op baseline migration and its own migrations-history table before the legacy all-table context is retired. Subsequent schema changes are owned by the relevant context.

Catalog adds a normalized `title_genres` association. Migration is expand-and-contract:

1. Add the association table and required indexes while retaining `titles.genre`.
2. Backfill associations by exact, trimmed, case-insensitive genre-name matching; report unmatched source tags.
3. Switch writes to associations and reads to a compatibility projection that preserves the existing comma-separated response value.
4. Verify parity in PostgreSQL integration tests and production diagnostics.
5. Drop the legacy column only in a later cleanup migration after at least one compatible deployment.

Alternative considered: restore schema-per-module immediately. This was rejected because schema movement is not needed to establish ownership and adds rollback risk. Tables may remain in `public`; ownership is enforced by DbContext mapping and architecture tests.

### 7. Preserve API contracts through explicit transport mapping

Endpoints retain their existing routes, authorization requirements, status mapping, request records, and response JSON. Application read models may initially remain the response models, but endpoint contract tests snapshot route metadata and representative JSON before internal types are reorganized.

Authentication claims are translated through one API-owned adapter before application commands are created. This refactor preserves the currently characterized failure contract; changing malformed or legacy ticket behavior is a separate product/API hardening change.

DI registration moves behind module registration methods called by `Program.cs`. Import CLI modes continue to use the same arguments, but invoke Catalog application operations and registered HTTP clients rather than mutating `CatalogDbContext` directly.

### 8. Migrate one vertical slice at a time

The first slice is Review because it has a small state boundary, a clear `Rating` invariant, and limited read/write paths. WatchProgress follows to establish time/numeric invariants and typed playable identity. Identity follows because concurrency and authorization make it higher risk. Catalog and its data migration come after the pattern has been proven. Analytics and Personalization are migrated last because they are append-heavy and algorithmic.

During each slice, compatibility adapters may implement an old interface by delegating to the new use case. The old interface is removed only after all callers move. This keeps the solution releasable and avoids one commit that changes every layer at once.

## Risks / Trade-offs

- [Namespace-level module boundaries are weaker than assembly boundaries] -> Enforce a dependency matrix in architecture tests and revisit assembly extraction only if violations or team size justify it.
- [Private constructors and typed IDs complicate EF materialization] -> Add explicit EF configurations/converters and verify materialization against PostgreSQL for every migrated aggregate.
- [Multiple DbContexts can accidentally lose transaction atomicity] -> Share the Npgsql connection/transaction only through an infrastructure transaction coordinator and cover hard delete and last-admin races with PostgreSQL tests.
- [Baseline migration histories can drift from deployed databases] -> Validate migrations from a fresh database and an upgraded copy, keep the legacy migration chain immutable, and fail CI on pending model changes.
- [Genre backfill can mis-map free-form source categories] -> Generate a deterministic unmatched-tag report, require zero unexplained rows before cutover, and retain the legacy column through a compatibility release.
- [Refactor can silently change HTTP serialization or error mapping] -> Capture endpoint metadata and representative response/error contract tests before moving handlers.
- [A large change can stall while both old and new abstractions coexist] -> Complete and remove compatibility code per vertical slice; tasks include explicit deletion checkpoints.
- [Additional abstractions may add ceremony to read paths] -> Keep direct projections and avoid repositories, domain events, or value objects where no invariant or ownership boundary exists.

## Migration Plan

1. Record baseline architecture, HTTP contracts, domain behavior, migration state, and PostgreSQL integration behavior.
2. Add the module dependency rules and shared application/infrastructure registration structure without moving behavior.
3. Migrate Review, WatchProgress, and SavedTitle slices; deploy after each completed slice.
4. Migrate Identity role/profile behavior and verify concurrent last-admin protection.
5. Add Catalog aggregate behavior and the additive title/genre association migration; deploy with the legacy column retained.
6. Migrate Analytics and Personalization ownership and remove cross-module DbSet access.
7. Introduce module DbContext baselines, repair cross-context references, add constraints, switch registrations/migration checks, then remove the legacy all-table context.
8. Replace Backoffice cross-table writes with module commands and the shared-transaction hard-delete coordinator.
9. Remove compatibility interfaces, unused packages, dead mappings, and stale architecture documentation.
10. After a compatibility release and verified data parity, schedule legacy-column removal as a separate cleanup migration.

Rollback uses the previous application build while additive schema remains in place. No destructive schema operation occurs in the same deployment that switches application reads or writes. Before any later contract step, take a database backup and verify the down migration against an upgraded database copy.
