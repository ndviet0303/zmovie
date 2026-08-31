namespace ZMovie.Domain.Catalog;

public readonly record struct TitleId(Guid Value) : IComparable<TitleId>, IComparable
{
    public static TitleId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(TitleId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is TitleId other ? CompareTo(other) : 1;
}

public readonly record struct EpisodeId(Guid Value) : IComparable<EpisodeId>, IComparable
{
    public static EpisodeId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(EpisodeId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is EpisodeId other ? CompareTo(other) : 1;
}

public readonly record struct GenreId(Guid Value) : IComparable<GenreId>, IComparable
{
    public static GenreId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(GenreId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is GenreId other ? CompareTo(other) : 1;
}
