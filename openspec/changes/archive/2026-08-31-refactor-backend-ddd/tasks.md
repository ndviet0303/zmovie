## 1. Baseline And Safety Nets

- [x] 1.1 Run the existing backend test suite and record the current route, migration, and test baseline before moving any production type.
- [x] 1.2 Add HTTP integration tests that lock representative success and error JSON, status codes, route templates, authorization requirements, and cookie authentication behavior for all endpoint groups.
- [x] 1.3 Add a PostgreSQL integration-test fixture that can migrate both a fresh database and an upgraded copy of the current schema; keep EF InMemory tests only for fast unit-level coverage.
- [x] 1.4 Add architecture tests for the existing project dependency rule and a documented allowlist that prevents new cross-context namespace dependencies during migration.
- [x] 1.5 Make the baseline build, unit tests, HTTP tests, and PostgreSQL migration tests run from the backend verification command and CI job.

## 2. Module Foundations

- [x] 2.1 Create the Catalog, Identity, Engagement, Analytics, Personalization, and Backoffice folder/namespace structure across Domain, Application, and Infrastructure without changing runtime behavior.
- [x] 2.2 Define and test the context dependency matrix: Domain isolation, inward-facing Application ports, contract-only cross-context calls, and API-only composition.
- [x] 2.3 Introduce module registration methods for Application and Infrastructure and move service registrations out of `Program.cs` while preserving service lifetimes.
- [x] 2.4 Register `TimeProvider` at the application boundary and establish the convention that domain decisions receive time explicitly.
- [x] 2.5 Add compatibility adapters for old interfaces only where a slice cannot move atomically, and document the deletion checkpoint for each adapter.

## 3. Engagement Review Slice

- [x] 3.1 Implement context-local typed identifiers, `Rating`, and `Review` domain behavior with private state and tests for create, edit, comment normalization, and invalid rating decisions.
- [x] 3.2 Add a Review aggregate repository and EF configuration that materialize private state and preserve the existing table, key, index, and timestamp behavior.
- [x] 3.3 Move submit and remove review handlers to the Review repository while keeping existing `ErrorOr` codes and HTTP response contracts.
- [x] 3.4 Keep review listing and average-rating calculation as direct query projections and cover their current ordering and rounding behavior.
- [x] 3.5 Route the admin review deletion through an Engagement-owned command instead of `IAdminStore`.
- [x] 3.6 Remove `ITitleReviewStore` and the review responsibilities from `EfUserLibraryStore` after all callers and tests use the new slice.

## 4. Engagement Library And Progress Slices

- [x] 4.1 Implement `SavedTitle` domain behavior and repository with the current idempotent-save and missing-remove semantics.
- [x] 4.2 Implement `WatchPosition` and `WatchProgress` domain behavior with explicit time, typed playable identity, and compatibility mapping for existing numeric/error behavior.
- [x] 4.3 Move save, remove, and progress command handlers to their aggregate repositories and retain Catalog lookup through an explicit Catalog contract.
- [x] 4.4 Keep user-library, history, continue-watching, and discovery responses as query projections; preserve ordering, deduplication, localization, and limits.
- [x] 4.5 Add domain, application, and PostgreSQL tests for idempotency, progress updates, multiple episodes, missing playables, and concurrent saves.
- [x] 4.6 Remove `IUserLibraryStore` and remaining library/progress methods from `EfUserLibraryStore` after callers migrate.

## 5. Identity And Access Slice

- [x] 5.1 Implement `User`, `ExternalIdentity`, and `Role` domain types without embedding Google or ASP.NET policy names in the Domain project.
- [x] 5.2 Move profile refresh and allowlist promotion behavior into Identity application/domain operations while preserving Google sign-in responses and role persistence.
- [x] 5.3 Implement `LastAdminPolicy` as an Identity domain decision and keep serializable transaction/concurrency mechanics in the Infrastructure repository.
- [x] 5.4 Replace `IUserIdentityStore` with Identity-owned repositories and queries, and adapt `GoogleIdentityVerifier` to the new application contracts.
- [x] 5.5 Move the ASP.NET authorization-policy constant to API composition and preserve the existing role claim and `/v1/admin` authorization behavior.
- [x] 5.6 Add PostgreSQL concurrency tests proving two simultaneous demotions cannot remove the last admin, plus tests for self-demotion and allowlist promotion.

## 6. Catalog Domain And Taxonomy

- [x] 6.1 Implement `Title`, localized metadata, `TitleSlug`, `TitleType`, release-year including explicit unknown source data, runtime, and featured-state behavior with domain tests.
- [x] 6.2 Implement `Episode` and `Genre` as separate Catalog aggregate roots with typed IDs and uniqueness policies backed by database constraints.
- [x] 6.3 Add Catalog aggregate repositories and move admin title, featured-state, genre create/update/delete, and concurrency handling to Catalog-owned commands.
- [x] 6.4 Convert OPhim and seed code into Catalog adapters that map source payloads through domain factories/methods instead of assigning public entity setters.
- [x] 6.5 Add the additive `title_genres` migration, indexes, deterministic backfill, unmatched-tag report, and down migration while retaining `titles.genre`.
- [x] 6.6 Switch Catalog writes to normalized genre assignments and switch reads to a compatibility projection that preserves the current comma-separated API field.
- [x] 6.7 Add PostgreSQL tests for title/episode/genre materialization, backfill parity, genre rename/delete rules, importer upserts, and optimistic-concurrency behavior.
- [x] 6.8 Remove direct Catalog entity mutation from `EfAdminStore`, importers, and seed code after all Catalog writes use the new commands.

## 7. Analytics Slice

- [x] 7.1 Move view facts, period calculation, and the 30-minute deduplication rule into Analytics domain/application types with explicit time and session/user identity.
- [x] 7.2 Implement Analytics-owned append/query adapters while retaining PostgreSQL advisory locking, current counting semantics, and response caching.
- [x] 7.3 Migrate record-view, view-count, and top-title handlers to Analytics contracts plus Catalog read contracts without sharing DbSets.
- [x] 7.4 Add PostgreSQL tests for concurrent duplicate views, anonymous versus authenticated identity, day/week/month boundaries in `Asia/Ho_Chi_Minh`, and top-title ordering.
- [x] 7.5 Remove analytics responsibilities from `EfUserLibraryStore` and delete the class when no responsibilities remain.

## 8. Personalization And Assistant Slice

- [x] 8.1 Implement Personalization impression and feedback domain types, allowed feedback events, and reward policy without depending on EF Core while preserving current repeated-feedback behavior.
- [x] 8.2 Add Personalization repositories and queries for impressions, feedback, and learned title scores while preserving current retention window and fallback behavior.
- [x] 8.3 Separate business ranking policy from model mathematics behind an application port; retain the existing TF-IDF implementation and output ordering unless contract tests show a regression.
- [x] 8.4 Move Assistant orchestration to explicit Catalog, Engagement, Personalization, and text-generator contracts with no direct shared-DbContext access.
- [x] 8.5 Add unit and PostgreSQL tests for impression ownership, feedback validation, score decay, unavailable-learning fallback, and unchanged assistant response contracts.
- [x] 8.6 Remove `AssistantLearningEvent`, `IAssistantLearningStore`, and cross-context responsibilities from the legacy Assistant infrastructure after migration.

## 9. Persistence Ownership And Migration Cutover

- [x] 9.1 Rename the existing all-table context to `LegacyCatalogDbContext` and create Catalog, Identity, Engagement, Analytics, and Personalization DbContexts whose configurations include only module-owned tables.
- [x] 9.2 Configure all module contexts from the same Npgsql data source with distinct migrations-history tables and compatible naming conventions.
- [x] 9.3 Create and verify no-op baseline migrations/model snapshots for each module without modifying the immutable legacy migration chain.
- [x] 9.4 Audit and repair orphan user/title/playable references, then add referencing-module foreign-key migrations without introducing cross-context EF navigations.
- [x] 9.5 Switch repositories, queries, development migration, and import migration paths to the module contexts.
- [x] 9.6 Test migration from an empty database and from a database at the current legacy head, and fail verification when any module has pending model changes.
- [x] 9.7 Remove `LegacyCatalogDbContext` only after a repository-wide check finds no cross-module DbSet access and all migration tests pass.

## 10. Backoffice And Atomic Cross-Context Operations

- [x] 10.1 Replace administration reads with `IAdminDashboardQueries` and focused query services that may compose read models but never mutate module data.
- [x] 10.2 Implement a shared-database transaction coordinator that enlists module DbContexts in one Npgsql transaction without exposing DbSets across contexts.
- [x] 10.3 Implement the hard-delete title coordinator using context-owned cleanup ports in the documented Engagement, Analytics, Personalization, then Catalog order.
- [x] 10.4 Add PostgreSQL tests proving hard deletion is atomic on success and rollback, removes every current dependent row, and preserves existing not-found behavior.
- [x] 10.5 Route every administration write to its owner-module command and keep existing endpoint request/response/status contracts.
- [x] 10.6 Remove `IAdminStore` and `EfAdminStore` after dashboard queries, owner commands, and the delete coordinator cover all former operations.

## 11. API Composition And Compatibility

- [x] 11.1 Add explicit endpoint transport mapping where internal domain/application types would otherwise leak into public request or response contracts.
- [x] 11.2 Consolidate claim-to-application-identity translation in an API-owned adapter and preserve the currently characterized failure contract for malformed or legacy claims.
- [x] 11.3 Make `Program.cs` call module registration/composition methods and route existing import CLI arguments through registered Catalog application operations and HTTP clients.
- [x] 11.4 Run HTTP contract tests across Catalog, Discovery, Search, Auth, Assistant, and Admin endpoints and resolve every unintended route, JSON, status, cookie, or policy difference.

## 12. Boundary Enforcement And Cleanup

- [x] 12.1 Replace the temporary architecture allowlist with final tests for all context and layer dependency rules, with zero unexplained exceptions.
- [x] 12.2 Remove compatibility adapters, obsolete stores/contracts, unused packages, dead EF mappings, and stale namespaces after verifying no runtime registrations reference them.
- [x] 12.3 Update backend architecture and development documentation with the context map, persistence ownership, command/query conventions, and migration procedure.
- [x] 12.4 Run formatting, build, the complete unit/application/infrastructure/HTTP/PostgreSQL test suite, coverage collection, and architecture tests.
- [x] 12.5 Verify genre data parity, module migration histories, fresh-install migration, upgrade migration, and rollback on a disposable PostgreSQL copy before marking the change complete.
