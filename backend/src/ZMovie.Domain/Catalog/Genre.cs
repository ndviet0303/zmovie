using ZMovie.Domain.Common;

namespace ZMovie.Domain.Catalog;

public sealed class Genre : AggregateRoot, IEntity<GenreId>
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
        var normalizedSlug = slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedName = name?.Trim() ?? string.Empty;

        var genre = new Genre
        {
            Id = id,
            Slug = normalizedSlug,
            Name = normalizedName,
            UpdatedAt = occurredAt,
        };

        genre.RaiseDomainEvent(new GenreCreatedDomainEvent(id, normalizedSlug, normalizedName, occurredAt));
        return genre;
    }

    public void Rename(string name, DateTimeOffset occurredAt)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        Name = normalizedName;
        UpdatedAt = occurredAt;

        RaiseDomainEvent(new GenreRenamedDomainEvent(Id, normalizedName, occurredAt));
    }
}
