namespace ZMovie.Domain.Catalog;

public sealed class TitleGenreAssignment
{
    private TitleGenreAssignment() { }

    public TitleId TitleId { get; private set; }
    public GenreId GenreId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    public static TitleGenreAssignment Create(TitleId titleId, GenreId genreId, DateTimeOffset assignedAt) =>
        new()
        {
            TitleId = titleId,
            GenreId = genreId,
            AssignedAt = assignedAt,
        };
}
