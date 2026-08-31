namespace ZMovie.Domain.Catalog;

public sealed class Genre
{
    private Genre() { }

    public GenreId Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Genre Create(
        GenreId id,
        string slug,
        string name,
        DateTimeOffset occurredAt)
    {
        return new Genre
        {
            Id = id,
            Slug = slug?.Trim().ToLowerInvariant() ?? string.Empty,
            Name = name?.Trim() ?? string.Empty,
            UpdatedAt = occurredAt,
        };
    }

    public void Rename(string name, DateTimeOffset occurredAt)
    {
        Name = name?.Trim() ?? string.Empty;
        UpdatedAt = occurredAt;
    }
}
