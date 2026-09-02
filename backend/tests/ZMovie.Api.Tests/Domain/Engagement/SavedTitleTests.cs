using System.Reflection;
using ZMovie.Domain.Engagement;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Engagement;

public sealed class SavedTitleTests
{
    private static readonly UserId UserId = new(Guid.Parse("67d06bf3-913b-4a55-b6ac-e758ecf9d0d7"));
    private static readonly TitleId TitleId = new(Guid.Parse("dd1fd52c-0fac-48bc-bf7c-eb48a7146791"));

    [Fact]
    public void Create_sets_identity_and_explicit_timestamp()
    {
        var savedAt = new DateTimeOffset(2026, 8, 31, 8, 15, 0, TimeSpan.Zero);

        var saved = SavedTitle.Create(UserId, TitleId, savedAt);

        Assert.Equal(UserId, saved.UserId);
        Assert.Equal(TitleId, saved.TitleId);
        Assert.Equal(savedAt, saved.SavedAt);
    }

    [Fact]
    public void Aggregate_has_only_a_private_parameterless_constructor_for_materialization()
    {
        var constructor = typeof(SavedTitle).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
        Assert.Empty(typeof(SavedTitle).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void Aggregate_state_can_only_be_changed_through_domain_behavior()
    {
        var stateProperties = typeof(SavedTitle).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.NotEmpty(stateProperties);
        Assert.All(stateProperties, property =>
        {
            Assert.NotNull(property.SetMethod);
            Assert.True(property.SetMethod.IsPrivate);
        });
    }
}
