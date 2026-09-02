# Backend Architecture & DDD Rules

ZMovie backend is a .NET 10 modular-monolith adhering to strict Domain-Driven Design (DDD) principles and a clean four-layer architecture.

---

## 1. Layer Dependency Rules

Unidirectional dependency flow must be preserved at all times:
```text
ZMovie.Domain  <---  ZMovie.Application  <---  ZMovie.Infrastructure  <---  ZMovie.Api
```

- **`ZMovie.Domain`**:
  - Pure domain models, aggregate roots, entities, value objects, domain events, domain rules.
  - Zero framework dependencies, zero NuGet packages (pure C#), zero wall-clock static calls (`DateTime.UtcNow`/`DateTime.Now`/`TimeProvider.System` are strictly forbidden; timestamps must be passed as method arguments).
  - Context isolation: Domain contexts must never reference other domain contexts. `ZMovie.Domain.Common` is the only shared kernel.
- **`ZMovie.Application`**:
  - Use cases, CQRS commands/queries (`ICommand<T>`, `IQuery<T>`), MediatR handlers, FluentValidation validators, error contracts (`ErrorOr<T>`), domain event handlers (`IDomainEventHandler<T>`), and repository/port interfaces.
  - References `ZMovie.Domain` only. `ZMovie.Application.Common` is the shared application kernel.
- **`ZMovie.Infrastructure`**:
  - Persistence adapters (EF Core with PostgreSQL), isolated `DbContext` per bounded context, `PublishDomainEventsInterceptor`, `MediatRDomainEventDispatcher`, search adapters, AI clients, caching, and background coordinators.
  - References `ZMovie.Application` and `ZMovie.Domain`.
- **`ZMovie.Api`**:
  - ASP.NET Core Minimal API endpoints, OpenAPI/Scalar, CORS, authentication cookies, claim translation adapters (`UserIdentityAdapter`), and application composition root.

---

## 2. Domain-Driven Design (DDD) Standards

### Aggregate Roots & Entities
- Aggregate roots must inherit from `AggregateRoot` (or implement `IAggregateRoot`) and implement `IEntity<TId>`.
- Entities must implement `IEntity<TId>`.
- Aggregates must encapsulate invariants:
  - Parameterless constructor must be `private`.
  - All public properties must have `private set`.
  - State mutations must occur through explicit, intention-revealing domain methods (e.g., `Create`, `UpdateMetadata`, `SetFeatured`, `RecordSignIn`, `ChangeRole`, `Edit`).
- When state changes occur within an aggregate, raise domain events via `RaiseDomainEvent(...)`.

### Domain Events
- All domain events must be `sealed record`s inheriting from `DomainEvent(DateTimeOffset OccurredAt)`.
- Event names follow the past-tense naming convention (e.g. `TitleCreatedDomainEvent`, `UserSignedInDomainEvent`, `ReviewSubmittedDomainEvent`).
- Handlers for domain events in Application implement `IDomainEventHandler<TDomainEvent>` (`INotificationHandler<DomainEventNotification<TDomainEvent>>`).

### Value Objects & Strongly-Typed IDs
- Use strongly-typed IDs (readonly record structs wrapping `Guid`, e.g. `TitleId`, `UserId`, `ReviewId`, `GenreId`, `EpisodeId`).
- Value objects must be immutable (e.g. `TitleSlug`, `ReleaseYear`, `Runtime`, `Rating`, `LocalizedText`, `Role`) and validate inputs via factory/`TryParse` methods.

---

## 3. Persistence & Interception

- Each bounded context owns an isolated `DbContext` (`CatalogDbContext`, `IdentityDbContext`, `EngagementDbContext`, `AnalyticsDbContext`, `PersonalizationDbContext`).
- Each `DbContext` must attach `PublishDomainEventsInterceptor` to collect and dispatch domain events before saving changes.
- Cross-context multi-entity workflows (such as Title hard deletion) use `ITransactionCoordinator` to enlist all participating DbContexts into a single PostgreSQL transaction.

---

## 4. Testing & Verification

Any backend modifications must satisfy:
1. `dotnet test backend/ZMovie.slnx` runs cleanly with 0 failures.
2. Architecture tests must pass:
   - `ArchitectureDependencyTests`: Verifies layer dependency directions and cross-context boundaries.
   - `DddArchitectureTests`: Verifies aggregate roots, domain events, and handlers.
   - `DomainTimeConventionTests`: Verifies zero static wall-clock usage in Domain.
   - `ModuleRegistrationTests`: Verifies DI lifetimes across modules.
