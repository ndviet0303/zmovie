using FluentAssertions;
using ZMovie.Domain.Personalization;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Personalization;

public sealed class PersonalizationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);
    private static readonly LearningEventId EventId = LearningEventId.New();
    private static readonly RecommendationId RecId = RecommendationId.New();
    private static readonly UserId UserId = UserId.New();
    private static readonly TitleId TitleId = TitleId.New();

    [Fact]
    public void RecordImpression_creates_learning_event_with_zero_reward_and_impression_type()
    {
        var evt = AssistantLearningEvent.RecordImpression(
            EventId,
            RecId,
            UserId,
            TitleId,
            "feature1,feature2",
            1,
            Now);

        evt.Id.Should().Be(EventId);
        evt.RecommendationId.Should().Be(RecId);
        evt.UserId.Should().Be(UserId);
        evt.TitleId.Should().Be(TitleId);
        evt.Features.Should().Be("feature1,feature2");
        evt.Rank.Should().Be(1);
        evt.EventType.Should().Be(FeedbackEventType.ImpressionName);
        evt.Reward.Should().Be(0.0);
        evt.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void RecordFeedback_creates_learning_event_with_matching_reward_and_event_type()
    {
        var evt = AssistantLearningEvent.RecordFeedback(
            EventId,
            RecId,
            UserId,
            TitleId,
            "feature1,feature2",
            2,
            FeedbackEventType.Like,
            4.0,
            Now);

        evt.EventType.Should().Be("like");
        evt.Reward.Should().Be(4.0);
        evt.Rank.Should().Be(2);

        var dislike = AssistantLearningEvent.RecordFeedback(
            EventId,
            RecId,
            UserId,
            TitleId,
            "f",
            1,
            FeedbackEventType.Dislike,
            -4.0,
            Now);
        dislike.Reward.Should().Be(-4.0);
    }

    [Fact]
    public void RecordFeedback_rejects_impression_as_feedback_event_type()
    {
        var act = () => AssistantLearningEvent.RecordFeedback(
            EventId,
            RecId,
            UserId,
            TitleId,
            "f",
            1,
            FeedbackEventType.Impression,
            0.0,
            Now);

        act.Should().Throw<ArgumentException>().WithMessage("*impression*");
    }

    [Fact]
    public void RecordImpression_and_RecordFeedback_validate_invariants()
    {
        var emptyEventId = () => AssistantLearningEvent.RecordImpression(new LearningEventId(Guid.Empty), RecId, UserId, TitleId, "f", 1, Now);
        emptyEventId.Should().Throw<ArgumentException>();

        var emptyRecId = () => AssistantLearningEvent.RecordImpression(EventId, new RecommendationId(Guid.Empty), UserId, TitleId, "f", 1, Now);
        emptyRecId.Should().Throw<ArgumentException>();

        var emptyUserId = () => AssistantLearningEvent.RecordImpression(EventId, RecId, new UserId(Guid.Empty), TitleId, "f", 1, Now);
        emptyUserId.Should().Throw<ArgumentException>();

        var emptyTitleId = () => AssistantLearningEvent.RecordImpression(EventId, RecId, UserId, new TitleId(Guid.Empty), "f", 1, Now);
        emptyTitleId.Should().Throw<ArgumentException>();

        var invalidRank = () => AssistantLearningEvent.RecordImpression(EventId, RecId, UserId, TitleId, "f", 0, Now);
        invalidRank.Should().Throw<ArgumentException>();

        var emptyFeatures = () => AssistantLearningEvent.RecordImpression(EventId, RecId, UserId, TitleId, "", 1, Now);
        emptyFeatures.Should().Throw<ArgumentException>();

        var tooLongFeatures = () => AssistantLearningEvent.RecordImpression(EventId, RecId, UserId, TitleId, new string('x', 2001), 1, Now);
        tooLongFeatures.Should().Throw<ArgumentException>();

        var defaultTime = () => AssistantLearningEvent.RecordImpression(EventId, RecId, UserId, TitleId, "f", 1, default);
        defaultTime.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FeedbackEventType_and_RewardPolicy_map_standard_and_reject_invalid()
    {
        FeedbackEventType.TryParse("click", out var click).Should().BeTrue();
        click.Should().Be(FeedbackEventType.Click);
        FeedbackEventType.TryParse("SAVE", out var save).Should().BeTrue();
        save.Should().Be(FeedbackEventType.Save);
        FeedbackEventType.TryParse("watch", out var watch).Should().BeTrue();
        watch.Should().Be(FeedbackEventType.Watch);
        FeedbackEventType.TryParse("complete", out var complete).Should().BeTrue();
        complete.Should().Be(FeedbackEventType.Complete);
        FeedbackEventType.TryParse("like", out var like).Should().BeTrue();
        like.Should().Be(FeedbackEventType.Like);
        FeedbackEventType.TryParse("dislike", out var dislike).Should().BeTrue();
        dislike.Should().Be(FeedbackEventType.Dislike);
        FeedbackEventType.TryParse("impression", out var impression).Should().BeTrue();
        impression.Should().Be(FeedbackEventType.Impression);
        impression.IsImpression.Should().BeTrue();

        FeedbackEventType.TryParse("invalid", out _).Should().BeFalse();
        FeedbackEventType.TryParse("", out _).Should().BeFalse();
        FeedbackEventType.TryParse(null, out _).Should().BeFalse();

        FeedbackEventType.Parse("click").Should().Be(FeedbackEventType.Click);
        var invalidParse = () => FeedbackEventType.Parse("unknown");
        invalidParse.Should().Throw<ArgumentException>();

        RewardPolicy.TryGetReward("click", out var clickReward).Should().BeTrue();
        clickReward.Should().Be(0.5);
        RewardPolicy.TryGetReward("save", out var saveReward).Should().BeTrue();
        saveReward.Should().Be(2.0);
        RewardPolicy.TryGetReward("watch", out var watchReward).Should().BeTrue();
        watchReward.Should().Be(3.0);
        RewardPolicy.TryGetReward("complete", out var completeReward).Should().BeTrue();
        completeReward.Should().Be(5.0);
        RewardPolicy.TryGetReward("like", out var likeReward).Should().BeTrue();
        likeReward.Should().Be(4.0);
        RewardPolicy.TryGetReward("dislike", out var dislikeReward).Should().BeTrue();
        dislikeReward.Should().Be(-4.0);
        RewardPolicy.TryGetReward("impression", out var impressionReward).Should().BeTrue();
        impressionReward.Should().Be(0.0);
        RewardPolicy.TryGetReward("unknown", out _).Should().BeFalse();
        RewardPolicy.TryGetReward(null, out _).Should().BeFalse();
    }
}
