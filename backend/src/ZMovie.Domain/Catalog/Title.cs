using ZMovie.Domain.Common;

namespace ZMovie.Domain.Catalog;

public sealed class Title : AggregateRoot, IEntity<TitleId>
{
    private Title() { }

    public TitleId Id { get; private set; }
    public TitleSlug Slug { get; private set; }
    public string VietnameseTitle { get; private set; } = string.Empty;
    public string EnglishTitle { get; private set; } = string.Empty;
    public string VietnameseSynopsis { get; private set; } = string.Empty;
    public string EnglishSynopsis { get; private set; } = string.Empty;
    public string Genre { get; private set; } = string.Empty;
    public ReleaseYear Year { get; private set; }
    public TitleType Type { get; private set; }
    public string PosterUrl { get; private set; } = string.Empty;
    public Runtime Runtime { get; private set; }
    public bool Featured { get; private set; }
    public string Actors { get; private set; } = string.Empty;
    public string Directors { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string TrailerUrl { get; private set; } = string.Empty;
    public bool IsR2Hosted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public LocalizedText TitleName
    {
        get => new(VietnameseTitle, EnglishTitle);
        private set
        {
            VietnameseTitle = value.Vietnamese;
            EnglishTitle = value.English;
        }
    }

    public LocalizedText Synopsis
    {
        get => new(VietnameseSynopsis, EnglishSynopsis);
        private set
        {
            VietnameseSynopsis = value.Vietnamese;
            EnglishSynopsis = value.English;
        }
    }

    public static Title Create(
        TitleId id,
        TitleSlug slug,
        LocalizedText titleName,
        LocalizedText synopsis,
        string genre,
        ReleaseYear year,
        TitleType type,
        string posterUrl,
        Runtime runtime,
        bool featured,
        DateTimeOffset occurredAt,
        string actors = "",
        string directors = "",
        string country = "",
        string trailerUrl = "",
        bool isR2Hosted = false)
    {
        var title = new Title
        {
            Id = id,
            Slug = slug,
            TitleName = titleName,
            Synopsis = synopsis,
            Genre = genre?.Trim() ?? string.Empty,
            Year = year,
            Type = type,
            PosterUrl = posterUrl?.Trim() ?? string.Empty,
            Runtime = runtime,
            Featured = featured,
            Actors = actors?.Trim() ?? string.Empty,
            Directors = directors?.Trim() ?? string.Empty,
            Country = country?.Trim() ?? string.Empty,
            TrailerUrl = trailerUrl?.Trim() ?? string.Empty,
            IsR2Hosted = isR2Hosted,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        title.RaiseDomainEvent(new TitleCreatedDomainEvent(id, slug, occurredAt));
        return title;
    }

    public void UpdateMetadata(
        LocalizedText titleName,
        LocalizedText synopsis,
        string genre,
        ReleaseYear year,
        TitleType type,
        string posterUrl,
        Runtime runtime,
        bool featured,
        DateTimeOffset occurredAt,
        string? actors = null,
        string? directors = null,
        string? country = null,
        string? trailerUrl = null,
        bool? isR2Hosted = null)
    {
        TitleName = titleName;
        Synopsis = synopsis;
        Genre = genre?.Trim() ?? string.Empty;
        Year = year;
        Type = type;
        PosterUrl = posterUrl?.Trim() ?? string.Empty;
        Runtime = runtime;
        Featured = featured;
        if (actors is not null) Actors = actors.Trim();
        if (directors is not null) Directors = directors.Trim();
        if (country is not null) Country = country.Trim();
        if (trailerUrl is not null) TrailerUrl = trailerUrl.Trim();
        if (isR2Hosted.HasValue) IsR2Hosted = isR2Hosted.Value;
        UpdatedAt = occurredAt;

        RaiseDomainEvent(new TitleMetadataUpdatedDomainEvent(Id, Slug, occurredAt));
    }

    public void SetFeatured(bool featured, DateTimeOffset occurredAt)
    {
        Featured = featured;
        UpdatedAt = occurredAt;

        RaiseDomainEvent(new TitleFeaturedChangedDomainEvent(Id, featured, occurredAt));
    }

    public string LocalizedTitle(string? locale) => TitleName.Localize(locale);
    public string LocalizedSynopsis(string? locale) => Synopsis.Localize(locale);
}
