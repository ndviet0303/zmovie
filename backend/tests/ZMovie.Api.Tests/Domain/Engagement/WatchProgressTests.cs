using System.Reflection;
using ZMovie.Domain.Engagement;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Engagement;

public sealed class WatchProgressTests
{
    private static readonly UserId UserId = new(Guid.Parse("67d06bf3-913b-4a55-b6ac-e758ecf9d0d7"));
    private static readonly PlayableId PlayableId = new(Guid.Parse("01993e3f-49db-7c77-bdc5-502ef26f28c3"));
    private static readonly TitleId TitleId = new(Guid.Parse("dd1fd52c-0fac-48bc-bf7c-eb48a7146791"));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(42.5, 42.5)]
    [InlineData(-10, 0)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    public void WatchPosition_normalizes_invalid_or_negative_values_to_zero(double input, double expected)
    {
        var position = WatchPosition.FromSeconds(input);

        Assert.Equal(expected, position.Seconds);
    }

    [Fact]
    public void Record_sets_identity_episode_position_and_timestamp()
    {
        var updatedAt = new DateTimeOffset(2026, 8, 31, 8, 15, 0, TimeSpan.Zero);
        var position = WatchPosition.FromSeconds(120.5);

        var progress = WatchProgress.Record(UserId, PlayableId, TitleId, 3, position, updatedAt);

        Assert.Equal(UserId, progress.UserId);
        Assert.Equal(PlayableId, progress.PlayableId);
        Assert.Equal(TitleId, progress.TitleId);
        Assert.Equal(3, progress.EpisodeNumber);
        Assert.Equal(position, progress.Position);
        Assert.Equal(updatedAt, progress.UpdatedAt);
    }

    [Fact]
    public void UpdateProgress_updates_episode_position_and_timestamp()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 8, 15, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddHours(2);
        var progress = WatchProgress.Record(UserId, PlayableId, TitleId, 1, WatchPosition.FromSeconds(10), createdAt);

        progress.UpdateProgress(2, WatchPosition.FromSeconds(45.5), updatedAt);

        Assert.Equal(2, progress.EpisodeNumber);
        Assert.Equal(45.5, progress.Position.Seconds);
        Assert.Equal(updatedAt, progress.UpdatedAt);
    }

    [Fact]
    public void Aggregate_has_only_a_private_parameterless_constructor_for_materialization()
    {
        var constructor = typeof(WatchProgress).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
        Assert.Empty(typeof(WatchProgress).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void Aggregate_state_can_only_be_changed_through_domain_behavior()
    {
        var stateProperties = typeof(WatchProgress).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.NotEmpty(stateProperties);
        Assert.All(stateProperties, property =>
        {
            Assert.NotNull(property.SetMethod);
            Assert.True(property.SetMethod.IsPrivate);
        });
    }
}
