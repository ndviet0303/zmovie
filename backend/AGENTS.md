# Backend Architecture & Project Guidelines

## 1. Project Dependencies & Component Guidelines
- Before building a feature, component, utility, or integration from scratch, first search for and evaluate existing packages, libraries, framework capabilities, and project dependencies that can solve the need.
- Prefer a maintained, well-supported existing solution when it meets the requirements; implement a custom solution only when the available options are unsuitable, introduce unacceptable trade-offs, or do not exist.
- When adding a dependency, choose the smallest appropriate package and avoid duplicating functionality already available in the codebase or its current dependencies.
- Before creating page-local UI, identify recurring layout or interaction patterns. Extract components that are reused or likely to be reused across routes (for example navigation, language controls, cards, filters, and dialogs); keep only truly page-specific markup in page files.

---

## 2. Four-Layer & Modular Monolith Rules
Unidirectional dependency flow:
```text
ZMovie.Domain  <---  ZMovie.Application  <---  ZMovie.Infrastructure  <---  ZMovie.Api
```

- **`ZMovie.Domain`**:
  - Pure domain models, aggregate roots, entities, value objects, domain events, and domain rules.
  - Zero framework dependencies, zero NuGet packages, zero static wall-clock calls (`DateTime.UtcNow`/`DateTime.Now`/`TimeProvider.System` are strictly forbidden; timestamps must be passed as arguments).
  - Context isolation: Domain contexts do not reference other domain contexts. `ZMovie.Domain.Common` is the only shared domain kernel.
- **`ZMovie.Application`**:
  - Use cases, CQRS commands/queries (`ICommand<T>`, `IQuery<T>`), MediatR handlers, FluentValidation, `ErrorOr<T>` outcomes, `IDomainEventHandler<T>`, and repository/port interfaces.
  - References `ZMovie.Domain` only. `ZMovie.Application.Common` is the shared application kernel.
- **`ZMovie.Infrastructure`**:
  - Persistence adapters (EF Core PostgreSQL DbContext per context), `PublishDomainEventsInterceptor`, `MediatRDomainEventDispatcher`, AI clients, search adapters, caching, and background coordinators.
  - References `ZMovie.Application` and `ZMovie.Domain`.
- **`ZMovie.Api`**:
  - ASP.NET Core Minimal APIs, routing, cookies, auth policies, `UserIdentityAdapter`, and composition root.

---

## 3. Domain-Driven Design (DDD) Conventions
- **Aggregates & Entities**:
  - Aggregate roots inherit `AggregateRoot` (or implement `IAggregateRoot`) and implement `IEntity<TId>`.
  - Parameterless constructors are `private`; all public properties have `private set`.
  - State changes occur strictly via domain methods (e.g. `Create`, `UpdateMetadata`, `SetFeatured`, `RecordSignIn`, `ChangeRole`, `Edit`).
  - State changes raise domain events using `RaiseDomainEvent(...)`.
- **Domain Events**:
  - Sealed records inheriting from `DomainEvent(DateTimeOffset OccurredAt)`.
  - Application handlers implement `IDomainEventHandler<TDomainEvent>` (`INotificationHandler<DomainEventNotification<TDomainEvent>>`).
- **Persistence Interceptor**:
  - Each `DbContext` attaches `PublishDomainEventsInterceptor` to collect, clear, and dispatch domain events before saving.
- **Transactions**:
  - Multi-context mutations coordinate through `ITransactionCoordinator` on a single PostgreSQL transaction.
