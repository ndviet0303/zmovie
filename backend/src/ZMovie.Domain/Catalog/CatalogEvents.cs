using ZMovie.Domain.Common;

namespace ZMovie.Domain.Catalog;

public sealed record TitleCreatedDomainEvent(
    TitleId TitleId,
    TitleSlug Slug,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record TitleMetadataUpdatedDomainEvent(
    TitleId TitleId,
    TitleSlug Slug,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record TitleFeaturedChangedDomainEvent(
    TitleId TitleId,
    bool Featured,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record EpisodeCreatedDomainEvent(
    EpisodeId EpisodeId,
    TitleId TitleId,
    int Number,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record EpisodeUpdatedDomainEvent(
    EpisodeId EpisodeId,
    TitleId TitleId,
    int Number,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record GenreCreatedDomainEvent(
    GenreId GenreId,
    string Slug,
    string Name,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record GenreRenamedDomainEvent(
    GenreId GenreId,
    string NewName,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
