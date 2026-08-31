namespace ZMovie.Domain.Engagement;

public sealed class SavedTitle
{
    private SavedTitle() { }

    private SavedTitle(UserId userId, TitleId titleId, DateTimeOffset savedAt)
    {
        UserId = userId;
        TitleId = titleId;
        SavedAt = savedAt;
    }

    public UserId UserId { get; private set; }
    public TitleId TitleId { get; private set; }
    public DateTimeOffset SavedAt { get; private set; }

    public static SavedTitle Create(UserId userId, TitleId titleId, DateTimeOffset savedAt) =>
        new(userId, titleId, savedAt);
}
