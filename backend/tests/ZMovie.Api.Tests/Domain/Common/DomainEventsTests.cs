using FluentAssertions;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Common;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;
using ZMovie.Domain.Personalization;
using Xunit;
using AnalyticsPlayableId = ZMovie.Domain.Analytics.PlayableId;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using EngagementPlayableId = ZMovie.Domain.Engagement.PlayableId;
using EngagementRating = ZMovie.Domain.Engagement.Rating;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;
using EngagementUserId = ZMovie.Domain.Engagement.UserId;
using IdentityRole = ZMovie.Domain.Identity.Role;
using IdentityUserId = ZMovie.Domain.Identity.UserId;
using PersonalizationFeedbackEventType = ZMovie.Domain.Personalization.FeedbackEventType;
using PersonalizationTitleId = ZMovie.Domain.Personalization.TitleId;
using PersonalizationUserId = ZMovie.Domain.Personalization.UserId;

namespace ZMovie.Api.Tests.Domain.Common;

public sealed class DomainEventsTests
{
    private static readonly DateTimeOffset SampleTime = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Title_raises_domain_events_on_lifecycle_changes()
    {
        var titleId = CatalogTitleId.New();
        var slug = TitleSlug.Parse("the-matrix");
        var titleName = new LocalizedText("Ma Trận", "The Matrix");
        var synopsis = new LocalizedText("Mô tả vi", "Description en");
        ReleaseYear.TryCreate(1999, out var year);
        Runtime.TryCreate(136, out var runtime);

        var title = Title.Create(
            titleId,
            slug,
            titleName,
            synopsis,
            "Action",
            year,
            TitleType.Movie,
            "https://cdn.example.com/poster.jpg",
            runtime,
            false,
            SampleTime);

        var aggregate = (IAggregateRoot)title;
        aggregate.DomainEvents.Should().ContainSingle(e => e is TitleCreatedDomainEvent);
        var createdEvent = (TitleCreatedDomainEvent)aggregate.DomainEvents.First();
        createdEvent.TitleId.Should().Be(titleId);
        createdEvent.Slug.Should().Be(slug);
        createdEvent.OccurredAt.Should().Be(SampleTime);

        aggregate.ClearDomainEvents();

        title.UpdateMetadata(
            titleName,
            synopsis,
            "Sci-Fi",
            year,
            TitleType.Movie,
            "https://cdn.example.com/poster2.jpg",
            runtime,
            true,
            SampleTime.AddHours(1));

        aggregate.DomainEvents.Should().ContainSingle(e => e is TitleMetadataUpdatedDomainEvent);

        aggregate.ClearDomainEvents();

        title.SetFeatured(false, SampleTime.AddHours(2));
        aggregate.DomainEvents.Should().ContainSingle(e => e is TitleFeaturedChangedDomainEvent);
    }

    [Fact]
    public void Genre_raises_domain_events_on_create_and_rename()
    {
        var genreId = GenreId.New();
        var genre = Genre.Create(genreId, "action", "Action", SampleTime);

        var aggregate = (IAggregateRoot)genre;
        aggregate.DomainEvents.Should().ContainSingle(e => e is GenreCreatedDomainEvent);

        aggregate.ClearDomainEvents();

        genre.Rename("Action & Adventure", SampleTime.AddDays(1));
        aggregate.DomainEvents.Should().ContainSingle(e => e is GenreRenamedDomainEvent);
    }

    [Fact]
    public void User_raises_domain_events_on_lifecycle_changes()
    {
        var userId = IdentityUserId.New();
        var externalId = new ExternalIdentity("google-sub-123");
        var user = User.Create(
            userId,
            externalId,
            "user@example.com",
            "Test User",
            null,
            IdentityRole.Member,
            SampleTime);

        var aggregate = (IAggregateRoot)user;
        aggregate.DomainEvents.Should().ContainSingle(e => e is UserCreatedDomainEvent);

        aggregate.ClearDomainEvents();

        user.RecordSignIn("user@example.com", "Test User Updated", null, SampleTime.AddHours(1));
        aggregate.DomainEvents.Should().ContainSingle(e => e is UserSignedInDomainEvent);

        aggregate.ClearDomainEvents();

        user.PromoteToAdmin();
        aggregate.DomainEvents.Should().ContainSingle(e => e is UserRoleChangedDomainEvent);
    }

    [Fact]
    public void Review_raises_domain_events_on_create_and_edit()
    {
        var reviewId = ReviewId.New();
        var titleId = EngagementTitleId.New();
        var userId = EngagementUserId.New();

        var decision = Review.Create(
            reviewId,
            titleId,
            userId,
            "Reviewer",
            8,
            "Great movie!",
            SampleTime);

        decision.IsAccepted.Should().BeTrue();
        var review = decision.Review!;
        var aggregate = (IAggregateRoot)review;
        aggregate.DomainEvents.Should().ContainSingle(e => e is ReviewSubmittedDomainEvent);

        aggregate.ClearDomainEvents();

        var editDecision = review.Edit("Reviewer", 9, "Updated comment", SampleTime.AddDays(1));
        editDecision.IsAccepted.Should().BeTrue();
        aggregate.DomainEvents.Should().ContainSingle(e => e is ReviewEditedDomainEvent);
    }

    [Fact]
    public void SavedTitle_and_WatchProgress_raise_domain_events()
    {
        var userId = EngagementUserId.New();
        var titleId = EngagementTitleId.New();
        var playableId = EngagementPlayableId.New();

        var savedTitle = SavedTitle.Create(userId, titleId, SampleTime);
        var savedAggregate = (IAggregateRoot)savedTitle;
        savedAggregate.DomainEvents.Should().ContainSingle(e => e is TitleSavedDomainEvent);

        var watchProgress = WatchProgress.Record(
            userId,
            playableId,
            titleId,
            1,
            WatchPosition.FromSeconds(120),
            SampleTime);

        var progressAggregate = (IAggregateRoot)watchProgress;
        progressAggregate.DomainEvents.Should().ContainSingle(e => e is WatchProgressRecordedDomainEvent);
    }

    [Fact]
    public void TitleViewEvent_raises_domain_events()
    {
        var eventId = ViewEventId.New();
        var titleId = AnalyticsTitleId.New();

        var viewEvent = TitleViewEvent.Record(
            eventId,
            titleId,
            null,
            null,
            "sess-abc",
            SampleTime);

        var aggregate = (IAggregateRoot)viewEvent;
        aggregate.DomainEvents.Should().ContainSingle(e => e is TitleViewRecordedDomainEvent);
    }

    [Fact]
    public void AssistantLearningEvent_raises_domain_events()
    {
        var eventId = LearningEventId.New();
        var recId = RecommendationId.New();
        var userId = PersonalizationUserId.New();
        var titleId = PersonalizationTitleId.New();

        var impression = AssistantLearningEvent.RecordImpression(
            eventId,
            recId,
            userId,
            titleId,
            "feat-vector",
            1,
            SampleTime);

        var impressionAggregate = (IAggregateRoot)impression;
        impressionAggregate.DomainEvents.Should().ContainSingle(e => e is AssistantImpressionRecordedDomainEvent);

        var feedback = AssistantLearningEvent.RecordFeedback(
            LearningEventId.New(),
            recId,
            userId,
            titleId,
            "feat-vector",
            1,
            PersonalizationFeedbackEventType.Click,
            1.0,
            SampleTime.AddMinutes(5));

        var feedbackAggregate = (IAggregateRoot)feedback;
        feedbackAggregate.DomainEvents.Should().ContainSingle(e => e is PersonalizationFeedbackRecordedDomainEvent);
    }
}
