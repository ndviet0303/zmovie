# Backend Context Dependency Matrix

This matrix specifies the architectural boundaries and context relationships for the ZMovie backend. Architecture tests (`ArchitectureDependencyTests` and `TemporaryCrossContextDependencyAllowlist`) strictly enforce these rules across all layers.

---

## Layer Dependency Rules

| Layer | Inbound From | Outbound Allowed | Enforced Rules |
|---|---|---|---|
| **Domain** | Application, Infrastructure | None | Pure C# types only. No framework references. No static wall clock. Cross-domain references prohibited. |
| **Application** | Infrastructure, API | Domain | MediatR handlers, validation, DTOs, read models, and ports. Cross-context calls must use explicit ports or read models. |
| **Infrastructure**| API | Domain, Application | Port implementations, EF Core DbContexts, PostgreSQL adapters. No cross-context DbSet sharing. |
| **API** | AppHost | Application, Infrastructure | Minimal APIs, route mappings, claim translation, problem details. No business logic. |

---

## Bounded Context Ownership

| Context | Domain Models | Application Operations | Persistence Tables |
|---|---|---|---|
| **Catalog** | `Title`, `Episode`, `Genre`, `TitleGenreAssignment` | `ListTitlesQuery`, `GetTitleQuery`, `GetPlaybackQuery`, `ICatalogReadStore`, `ITitleRepository`, `IEpisodeRepository`, `IGenreRepository` | `titles`, `episodes`, `genres`, `title_genres` |
| **Identity & Access** | `User`, `ExternalIdentity`, `Role`, `LastAdminPolicy` | `SignInWithGoogleCommand`, `IUserRepository`, `IUserQueries`, `IGoogleIdentityVerifier` | `users` |
| **Engagement** | `SavedTitle`, `WatchProgress`, `Review` | `SaveTitleCommand`, `RecordWatchProgressCommand`, `SubmitTitleReviewCommand`, `ISavedTitleRepository`, `IWatchProgressRepository`, `IReviewRepository` | `saved_titles`, `watch_history`, `title_reviews` |
| **Analytics** | `TitleViewEvent` | `RecordTitleViewCommand`, `GetTopTitlesQuery`, `IViewEventRepository`, `IViewAnalyticsQueries` | `title_view_events` |
| **Personalization** | `AssistantLearningEvent`, `SearchMoodWeights` | `RecordPersonalizationFeedbackCommand`, `IPersonalizationLearningRepository`, `IPersonalizationQueries` | `assistant_learning_events` |
| **Assistant** | - | `AskCatalogAssistantQuery`, `GetAssistantContextQuery`, `IAssistantTextGenerator`, `TinyContentRecommendationEngine` | - |
| **Search** | - | `SearchCatalogQuery`, `ISearchCatalogStore` | - |
| **Administration** | - | `IAdminDashboardQueries`, `IAdminTitleDeletionCoordinator`, `ICatalogAdministrationService`, `ITransactionCoordinator` | - |

---

## Cross-Context Ports and Read Models

Where contexts collaborate, they communicate through explicit application ports or immutable read models:

1. **Catalog Lookup for Engagement & Personalization**:
   - `ILibraryCatalogReader` (implemented in Catalog infrastructure) provides lightweight title summaries to library and personalization flows.
2. **Top Titles & Discovery Projections**:
   - Analytics aggregates view counts and delegates catalog metadata enrichment to `ILibraryCatalogReader`.
3. **Administration Orchestration**:
   - `IAdminDashboardQueries`: Reads across contexts for the unified admin dashboard.
   - `IAdminTitleDeletionCoordinator`: Executes atomic cascade deletions across `Engagement`, `Analytics`, `Personalization`, and `Catalog`.
