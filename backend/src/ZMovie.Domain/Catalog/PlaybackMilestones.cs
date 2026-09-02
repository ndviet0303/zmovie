namespace ZMovie.Domain.Catalog;

public readonly record struct PlaybackMilestones(int? IntroStart, int? IntroEnd, int? OutroStart, int? OutroEnd)
{
    public static PlaybackMilestones None => new(null, null, null, null);

    public bool HasIntro => IntroStart.HasValue && IntroEnd.HasValue && IntroEnd > IntroStart;
    public bool HasOutro => OutroStart.HasValue && OutroEnd.HasValue && OutroEnd > OutroStart;
}
