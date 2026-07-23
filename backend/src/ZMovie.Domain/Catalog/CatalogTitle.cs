namespace ZMovie.Domain.Catalog;

public sealed class CatalogEpisode
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TitleId { get; init; }
    public int Number { get; init; }
    public required string Name { get; set; }
    public required string HlsUrl { get; set; }
}

public sealed class CatalogTitle
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Slug { get; init; }
    public required string EnglishTitle { get; set; }
    public required string VietnameseTitle { get; set; }
    public required string EnglishSynopsis { get; set; }
    public required string VietnameseSynopsis { get; set; }
    public required string Genre { get; set; }
    public int Year { get; set; }
    public required string Type { get; set; }
    public required string PosterUrl { get; set; }
    public int RuntimeMinutes { get; set; }
    public bool Featured { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string LocalizedTitle(string locale) => locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? EnglishTitle : VietnameseTitle;
    public string LocalizedSynopsis(string locale) => locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? EnglishSynopsis : VietnameseSynopsis;
}
