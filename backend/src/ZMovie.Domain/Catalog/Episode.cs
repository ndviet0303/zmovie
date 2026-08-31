namespace ZMovie.Domain.Catalog;

public sealed class Episode
{
    private Episode() { }

    public EpisodeId Id { get; private set; }
    public TitleId TitleId { get; private set; }
    public int Number { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string HlsUrl { get; private set; } = string.Empty;

    public static Episode Create(
        EpisodeId id,
        TitleId titleId,
        int number,
        string name,
        string hlsUrl)
    {
        return new Episode
        {
            Id = id,
            TitleId = titleId,
            Number = number,
            Name = name?.Trim() ?? string.Empty,
            HlsUrl = hlsUrl?.Trim() ?? string.Empty,
        };
    }

    public void Update(string name, string hlsUrl)
    {
        Name = name?.Trim() ?? string.Empty;
        HlsUrl = hlsUrl?.Trim() ?? string.Empty;
    }
}
