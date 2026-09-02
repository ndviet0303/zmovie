using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public sealed class SavedTitle : AggregateRoot
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

    public static SavedTitle Create(UserId userId, TitleId titleId, DateTimeOffset savedAt)
    {
        var savedTitle = new SavedTitle(userId, titleId, savedAt);
        savedTitle.RaiseDomainEvent(new TitleSavedDomainEvent(userId, titleId, savedAt));
        return savedTitle;
    }
}
