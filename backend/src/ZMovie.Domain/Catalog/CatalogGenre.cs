namespace ZMovie.Domain.Catalog;

public sealed class CatalogGenre
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
