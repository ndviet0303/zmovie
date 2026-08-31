namespace ZMovie.Domain.Catalog;

public sealed class Title
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
        DateTimeOffset occurredAt)
    {
        return new Title
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
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };
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
        DateTimeOffset occurredAt)
    {
        TitleName = titleName;
        Synopsis = synopsis;
        Genre = genre?.Trim() ?? string.Empty;
        Year = year;
        Type = type;
        PosterUrl = posterUrl?.Trim() ?? string.Empty;
        Runtime = runtime;
        Featured = featured;
        UpdatedAt = occurredAt;
    }

    public void SetFeatured(bool featured, DateTimeOffset occurredAt)
    {
        Featured = featured;
        UpdatedAt = occurredAt;
    }

    public string LocalizedTitle(string? locale) => TitleName.Localize(locale);
    public string LocalizedSynopsis(string? locale) => Synopsis.Localize(locale);
}
