using ZMovie.Domain.Common;

namespace ZMovie.Domain.Catalog;

public sealed class Episode : IEntity<EpisodeId>
{
    private readonly List<EpisodeStreamSource> _sources = [];

    private Episode() { }

    public EpisodeId Id { get; private set; }
    public TitleId TitleId { get; private set; }
    public int Number { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string HlsUrl { get; private set; } = string.Empty;
    public string SubtitleUrl { get; private set; } = string.Empty;

    public int? IntroStart { get; private set; }
    public int? IntroEnd { get; private set; }
    public int? OutroStart { get; private set; }
    public int? OutroEnd { get; private set; }

    public PlaybackMilestones Milestones
    {
        get => new(IntroStart, IntroEnd, OutroStart, OutroEnd);
        private set
        {
            IntroStart = value.IntroStart;
            IntroEnd = value.IntroEnd;
            OutroStart = value.OutroStart;
            OutroEnd = value.OutroEnd;
        }
    }

    public IReadOnlyCollection<EpisodeStreamSource> Sources
    {
        get => _sources.AsReadOnly();
        private set
        {
            _sources.Clear();
            if (value is not null) _sources.AddRange(value);
        }
    }

    public static Episode Create(
        EpisodeId id,
        TitleId titleId,
        int number,
        string name,
        string hlsUrl,
        string subtitleUrl = "",
        PlaybackMilestones? milestones = null)
    {
        var episode = new Episode
        {
            Id = id,
            TitleId = titleId,
            Number = number,
            Name = name?.Trim() ?? string.Empty,
            HlsUrl = hlsUrl?.Trim() ?? string.Empty,
            SubtitleUrl = subtitleUrl?.Trim() ?? string.Empty,
            Milestones = milestones ?? PlaybackMilestones.None,
        };

        if (!string.IsNullOrWhiteSpace(hlsUrl))
        {
            episode.AddSource(EpisodeStreamSource.Create(
                EpisodeSourceId.New(),
                id,
                "Primary",
                hlsUrl,
                StreamFormat.Infer(hlsUrl),
                1,
                true,
                subtitleUrl));
        }

        return episode;
    }

    public void Update(string name, string hlsUrl, string? subtitleUrl = null)
    {
        Name = name?.Trim() ?? string.Empty;
        HlsUrl = hlsUrl?.Trim() ?? string.Empty;
        if (subtitleUrl is not null) SubtitleUrl = subtitleUrl.Trim();
    }

    public void SetMilestones(PlaybackMilestones milestones)
    {
        Milestones = milestones;
    }

    public void AddSource(EpisodeStreamSource source)
    {
        if (_sources.All(s => s.Id != source.Id))
        {
            _sources.Add(source);
        }
    }

    public void RemoveSource(EpisodeSourceId sourceId)
    {
        _sources.RemoveAll(s => s.Id == sourceId);
    }
}
