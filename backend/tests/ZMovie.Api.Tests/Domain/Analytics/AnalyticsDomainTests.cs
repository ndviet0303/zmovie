using System.Reflection;
using FluentAssertions;
using ZMovie.Domain.Analytics;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Analytics;

public sealed class AnalyticsDomainTests
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 8, 31, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public void Record_creates_view_event_for_authenticated_user()
    {
        var id = ViewEventId.New();
        var titleId = TitleId.New();
        var userId = UserId.New();

        var view = TitleViewEvent.Record(id, titleId, 2, userId, "session-1", NowUtc);

        view.Id.Should().Be(id);
        view.TitleId.Should().Be(titleId);
        view.EpisodeNumber.Should().Be(2);
        view.UserId.Should().Be(userId);
        view.SessionId.Should().Be("session-1");
        view.ViewedAt.Should().Be(NowUtc);
    }

    [Fact]
    public void Record_creates_view_event_for_anonymous_user()
    {
        var id = ViewEventId.New();
        var titleId = TitleId.New();

        var view = TitleViewEvent.Record(id, titleId, null, null, "anon-session", NowUtc);

        view.Id.Should().Be(id);
        view.TitleId.Should().Be(titleId);
        view.EpisodeNumber.Should().BeNull();
        view.UserId.Should().BeNull();
        view.SessionId.Should().Be("anon-session");
        view.ViewedAt.Should().Be(NowUtc);
    }

    [Fact]
    public void Record_rejects_anonymous_view_without_session_id()
    {
        var action = () => TitleViewEvent.Record(
            ViewEventId.New(), TitleId.New(), null, null, "   ", NowUtc);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void Record_rejects_negative_or_zero_episode_number()
    {
        var zero = () => TitleViewEvent.Record(
            ViewEventId.New(), TitleId.New(), 0, null, "sess", NowUtc);
        zero.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("episodeNumber");

        var negative = () => TitleViewEvent.Record(
            ViewEventId.New(), TitleId.New(), -1, null, "sess", NowUtc);
        negative.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("episodeNumber");
    }

    [Fact]
    public void Record_rejects_default_timestamp()
    {
        var action = () => TitleViewEvent.Record(
            ViewEventId.New(), TitleId.New(), null, null, "sess", default);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("viewedAt");
    }

    [Fact]
    public void Aggregate_roots_have_private_parameterless_constructors_and_private_setters()
    {
        var type = typeof(TitleViewEvent);
        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
        Assert.Empty(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.All(properties, p =>
        {
            Assert.NotNull(p.SetMethod);
            Assert.True(p.SetMethod.IsPrivate);
        });
    }

    [Fact]
    public void DeduplicationPolicy_computes_threshold_30_minutes_prior()
    {
        DeduplicationPolicy.DeduplicationWindow.Should().Be(TimeSpan.FromMinutes(30));

        var threshold = DeduplicationPolicy.DeduplicationThreshold(NowUtc);
        threshold.Should().Be(NowUtc.AddMinutes(-30));
    }

    [Theory]
    // 2026-08-31 is a Monday. 12:34:56 UTC is 19:34:56 in ICT (UTC+7).
    // Day starts at 2026-08-31 00:00:00 ICT => 2026-08-30 17:00:00 UTC.
    // Week starts at Monday 2026-08-31 00:00:00 ICT => 2026-08-30 17:00:00 UTC.
    // Month starts at 2026-08-01 00:00:00 ICT => 2026-07-31 17:00:00 UTC.
    [InlineData(TopPeriod.Day, "2026-08-30T17:00:00+00:00")]
    [InlineData(TopPeriod.Week, "2026-08-30T17:00:00+00:00")]
    [InlineData(TopPeriod.Month, "2026-07-31T17:00:00+00:00")]
    public void PeriodCalculator_computes_boundaries_in_vietnam_time(TopPeriod period, string expectedUtc)
    {
        var start = PeriodCalculator.CalculatePeriodStart(period, NowUtc);
        start.Should().Be(DateTimeOffset.Parse(expectedUtc));
    }

    [Fact]
    public void PeriodCalculator_computes_week_start_midweek_correctly()
    {
        // Thursday 2026-09-03 10:00:00 UTC => 17:00:00 ICT
        // Week start is Monday 2026-08-31 00:00:00 ICT => 2026-08-30 17:00:00 UTC
        var midWeekUtc = new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);
        var start = PeriodCalculator.CalculatePeriodStart(TopPeriod.Week, midWeekUtc);
        start.Should().Be(new DateTimeOffset(2026, 8, 30, 17, 0, 0, TimeSpan.Zero));
    }
}
