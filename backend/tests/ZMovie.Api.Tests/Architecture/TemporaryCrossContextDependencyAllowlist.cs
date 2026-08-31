namespace ZMovie.Api.Tests.Architecture;

internal readonly record struct NamespaceDependency(string Source, string Target)
{
    public override string ToString() => $"{Source} -> {Target}";
}

internal sealed record TemporaryNamespaceDependency(
    NamespaceDependency Dependency,
    string Reason,
    string RemovalCheckpoint);

internal readonly record struct CrossContextTypeDependency(string Source, string Target)
{
    public override string ToString() => $"{Source} -> {Target}";
}

internal enum CrossContextTypeDependencyKind
{
    Contract,
    ReadModel,
    TemporaryViolation,
}

internal sealed record TemporaryTypeDependency(
    CrossContextTypeDependency Dependency,
    CrossContextTypeDependencyKind Kind,
    string Reason,
    string RemovalCheckpoint);

internal static class TemporaryCrossContextDependencyAllowlist
{
    // This is an exact baseline, not a list of generally permitted dependencies. The
    // architecture test fails when an edge is added or removed so each migration step must
    // deliberately update this file and its documented deletion checkpoint.
    public static IReadOnlyList<TemporaryNamespaceDependency> Entries { get; } =
    [
        Edge("ZMovie.Application.Administration", "ZMovie.Application.Analytics", "Admin delete title coordinator calls Analytics cleanup port.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Application.Catalog", "Admin delete title coordinator calls Catalog cleanup port.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Application.Engagement", "Admin delete title coordinator calls Engagement cleanup port.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Application.Identity", "Admin role change calls Identity repository.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Application.Personalization", "Admin delete title coordinator calls Personalization cleanup port.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Domain.Analytics", "Admin delete title coordinator translates typed TitleId for Analytics.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Domain.Engagement", "Admin delete title coordinator translates typed TitleId for Engagement.", "Backoffice slice 10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Domain.Identity", "Backoffice evaluates role rules and creates typed user IDs.", "Identity and Backoffice slices 5.x/10.x"),
        Edge("ZMovie.Application.Administration", "ZMovie.Domain.Personalization", "Admin delete title coordinator translates typed TitleId for Personalization.", "Backoffice slice 10.x"),

        Edge("ZMovie.Application.Analytics", "ZMovie.Application.Catalog", "Analytics handlers project top title summaries.", "Boundary cleanup 12.x"),
        Edge("ZMovie.Application.Analytics", "ZMovie.Application.Engagement", "Analytics handlers resolve titles through the catalog reader port.", "Boundary cleanup 12.x"),
        Edge("ZMovie.Application.Assistant", "ZMovie.Application.Catalog", "Assistant responses reuse Catalog read contracts.", "Personalization slice 8.x"),
        Edge("ZMovie.Application.Assistant", "ZMovie.Application.Personalization", "Assistant handlers orchestrate impressions via Personalization port.", "Personalization slice 8.x"),
        Edge("ZMovie.Application.Engagement", "ZMovie.Application.Catalog", "Engagement read models reuse Catalog title summaries.", "Engagement slices 3.x-4.x"),
        Edge("ZMovie.Application.Personalization", "ZMovie.Application.Engagement", "Personalization feedback resolves titles through an Engagement-owned port.", "Personalization slice 8.x"),
        Edge("ZMovie.Application.Search", "ZMovie.Application.Catalog", "Search returns the current Catalog list contract.", "Boundary cleanup 12.x"),

        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Domain.Catalog", "Dashboard queries read Catalog title and genre entities.", "Catalog and Backoffice slices 6.x/10.x"),
        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Domain.Engagement", "Dashboard queries read Review entities and rating buckets.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Domain.Identity", "Dashboard queries read Identity entities.", "Identity and Backoffice slices 5.x/10.x"),
        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Infrastructure.Analytics", "Dashboard queries read Analytics module context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Infrastructure.Catalog", "Dashboard queries read Catalog module context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Infrastructure.Engagement", "Dashboard queries read Engagement module context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Administration", "ZMovie.Infrastructure.Identity", "Dashboard queries read Identity module context.", "Backoffice slice 10.x"),

        Edge("ZMovie.Infrastructure.Assistant", "ZMovie.Application.Catalog", "Assistant infrastructure reuses Catalog response models.", "Personalization slice 8.x"),
        Edge("ZMovie.Infrastructure.Assistant", "ZMovie.Application.Engagement", "Assistant ranking reads Engagement history and recommendations.", "Personalization slice 8.x"),
        Edge("ZMovie.Infrastructure.Assistant", "ZMovie.Application.Personalization", "Assistant ranking queries Personalization learned title scores.", "Personalization slice 8.x"),
        Edge("ZMovie.Infrastructure.Assistant", "ZMovie.Domain.Engagement", "Assistant passes typed UserId to Engagement history query port.", "Personalization slice 8.x"),
        Edge("ZMovie.Infrastructure.Assistant", "ZMovie.Domain.Personalization", "Assistant passes typed UserId to Personalization query port.", "Personalization slice 8.x"),

        Edge("ZMovie.Infrastructure.Catalog", "ZMovie.Application.Administration", "Catalog admin service implements the administration mutation port.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Catalog", "ZMovie.Application.Analytics", "Catalog read store queries title view counts.", "Boundary cleanup 12.x"),
        Edge("ZMovie.Infrastructure.Catalog", "ZMovie.Application.Engagement", "Catalog adapters implement Engagement lookup and analytics ports.", "Engagement and Analytics slices 3.x-7.x"),
        Edge("ZMovie.Infrastructure.Catalog", "ZMovie.Domain.Analytics", "Catalog read store passes typed TitleId to Analytics query port.", "Boundary cleanup 12.x"),
        Edge("ZMovie.Infrastructure.Catalog", "ZMovie.Infrastructure.Administration", "Catalog admin service delegates detail projections to dashboard queries.", "Backoffice slice 10.x"),

        Edge("ZMovie.Infrastructure.Common", "ZMovie.Infrastructure.Analytics", "Transaction coordinator enlists Analytics context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Common", "ZMovie.Infrastructure.Catalog", "Transaction coordinator enlists Catalog context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Common", "ZMovie.Infrastructure.Engagement", "Transaction coordinator enlists Engagement context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Common", "ZMovie.Infrastructure.Identity", "Transaction coordinator enlists Identity context.", "Backoffice slice 10.x"),
        Edge("ZMovie.Infrastructure.Common", "ZMovie.Infrastructure.Personalization", "Transaction coordinator enlists Personalization context.", "Backoffice slice 10.x"),

        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Domain.Analytics", "The legacy DbContext maps Analytics TitleViewEvent entities.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Domain.Catalog", "The legacy DbContext maps Catalog entities.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Domain.Engagement", "The legacy DbContext maps Engagement and learning entities.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Domain.Identity", "The legacy DbContext maps Identity entities.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Domain.Personalization", "The legacy DbContext maps Personalization entities.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Infrastructure.Analytics", "The legacy DbContext applies Analytics EF configuration.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Infrastructure.Catalog", "The legacy DbContext applies Catalog EF configuration.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Infrastructure.Engagement", "The legacy DbContext applies Engagement EF configuration.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Infrastructure.Identity", "The legacy DbContext applies Identity EF configuration.", "Persistence cutover 9.x"),
        Edge("ZMovie.Infrastructure.Persistence", "ZMovie.Infrastructure.Personalization", "The legacy DbContext applies Personalization EF configuration.", "Persistence cutover 9.x"),

        Edge("ZMovie.Infrastructure.Personalization", "ZMovie.Application.Assistant", "Personalization impression recorder reads search mood weights.", "Boundary cleanup 12.x"),
        Edge("ZMovie.Infrastructure.Personalization", "ZMovie.Application.Engagement", "Personalization impression recorder resolves title IDs via catalog reader.", "Boundary cleanup 12.x"),

        Edge("ZMovie.Infrastructure.Recommendations", "ZMovie.Application.Engagement", "The recommendation implementation consumes Engagement model contracts.", "Personalization slice 8.x"),
        Edge("ZMovie.Infrastructure.Search", "ZMovie.Application.Catalog", "Search projects results into the Catalog list contract.", "Boundary cleanup 12.x"),
        Edge("ZMovie.Infrastructure.Search", "ZMovie.Infrastructure.Catalog", "Search fallback queries CatalogDbContext.", "Catalog and persistence cutover 6.x/9.x"),
        Edge("ZMovie.Infrastructure.Seed", "ZMovie.Domain.Catalog", "Seed code constructs Catalog persistence entities directly.", "Catalog slice 6.x"),
        Edge("ZMovie.Infrastructure.Seed", "ZMovie.Infrastructure.Catalog", "Seed code writes to CatalogDbContext.", "Catalog and persistence cutover 6.x/9.x"),
    ];

    // Unlike the namespace baseline above, these entries state which Application-facing
    // types may cross a context boundary. A namespace edge is not permission to consume
    // another context's aggregates or arbitrary implementation types.
    public static IReadOnlyList<TemporaryTypeDependency> ApplicationTypeEntries { get; } =
    [
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Application.Analytics.IAnalyticsTitleCleanupPort", CrossContextTypeDependencyKind.Contract, "Admin delete title coordinator calls Analytics cleanup port.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Application.Catalog.ICatalogTitleCleanupPort", CrossContextTypeDependencyKind.Contract, "Admin delete title coordinator calls Catalog cleanup port.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Application.Engagement.IEngagementTitleCleanupPort", CrossContextTypeDependencyKind.Contract, "Admin delete title coordinator calls Engagement cleanup port.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Application.Identity.IUserRepository", CrossContextTypeDependencyKind.Contract, "Admin role change calls Identity repository.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Application.Identity.SetRoleOutcome", CrossContextTypeDependencyKind.Contract, "Admin role change evaluates Identity role outcome.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Application.Personalization.IPersonalizationTitleCleanupPort", CrossContextTypeDependencyKind.Contract, "Admin delete title coordinator calls Personalization cleanup port.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Domain.Analytics.TitleId", CrossContextTypeDependencyKind.TemporaryViolation, "Admin delete title coordinator translates typed TitleId for Analytics.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Domain.Engagement.TitleId", CrossContextTypeDependencyKind.TemporaryViolation, "Admin delete title coordinator translates typed TitleId for Engagement.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Domain.Identity.Role", CrossContextTypeDependencyKind.TemporaryViolation, "Backoffice directly consumes the Identity Role value object.", "Identity slice 5.x"),
        TypeEdge("ZMovie.Application.Administration", "ZMovie.Domain.Personalization.TitleId", CrossContextTypeDependencyKind.TemporaryViolation, "Admin delete title coordinator translates typed TitleId for Personalization.", "Backoffice slice 10.x"),
        TypeEdge("ZMovie.Application.Analytics", "ZMovie.Application.Catalog.TitleSummary", CrossContextTypeDependencyKind.ReadModel, "Analytics composes top-title projections from Catalog summaries.", "Boundary cleanup 12.x"),
        TypeEdge("ZMovie.Application.Analytics", "ZMovie.Application.Engagement.ILibraryCatalogReader", CrossContextTypeDependencyKind.Contract, "Analytics temporarily calls through the existing catalog lookup port.", "Boundary cleanup 12.x"),
        TypeEdge("ZMovie.Application.Analytics", "ZMovie.Application.Engagement.LibraryTitle", CrossContextTypeDependencyKind.ReadModel, "Analytics top title query projects through the catalog title model.", "Boundary cleanup 12.x"),
        TypeEdge("ZMovie.Application.Assistant", "ZMovie.Application.Catalog.TitleSummary", CrossContextTypeDependencyKind.ReadModel, "Assistant returns the current Catalog summary projection.", "Personalization slice 8.x"),
        TypeEdge("ZMovie.Application.Assistant", "ZMovie.Application.Personalization.IAssistantImpressionRecorder", CrossContextTypeDependencyKind.Contract, "Assistant queries optionally record impressions.", "Personalization slice 8.x"),
        TypeEdge("ZMovie.Application.Engagement", "ZMovie.Application.Catalog.TitleSummary", CrossContextTypeDependencyKind.ReadModel, "Engagement composes discovery and top-title projections from Catalog summaries.", "Engagement slices 3.x-4.x"),
        TypeEdge("ZMovie.Application.Personalization", "ZMovie.Application.Engagement.ILibraryCatalogReader", CrossContextTypeDependencyKind.Contract, "Personalization feedback handler resolves title IDs via catalog reader.", "Boundary cleanup 12.x"),
        TypeEdge("ZMovie.Application.Search", "ZMovie.Application.Catalog.TitleListResponse", CrossContextTypeDependencyKind.ReadModel, "Search is a Catalog-oriented read capability and returns its list projection.", "Boundary cleanup 12.x"),
    ];

    private static TemporaryNamespaceDependency Edge(
        string source,
        string target,
        string reason,
        string removalCheckpoint) =>
        new(new NamespaceDependency(source, target), reason, removalCheckpoint);

    private static TemporaryTypeDependency TypeEdge(
        string source,
        string target,
        CrossContextTypeDependencyKind kind,
        string reason,
        string removalCheckpoint) =>
        new(new CrossContextTypeDependency(source, target), kind, reason, removalCheckpoint);
}
