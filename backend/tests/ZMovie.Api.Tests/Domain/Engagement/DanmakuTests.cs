using System.Reflection;
using FluentAssertions;
using Xunit;
using ZMovie.Domain.Engagement;

namespace ZMovie.Api.Tests.Domain.Engagement;

public sealed class DanmakuTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DanmakuComment_creates_with_valid_attributes()
    {
        var id = DanmakuCommentId.New();
        var comment = DanmakuComment.Create(
            id,
            "natra-2",
            1,
            45,
            "Phim đỉnh quá anh em ơi! 🔥",
            "#f59e0b",
            null,
            "ZMovieFan",
            Now);

        comment.Id.Should().Be(id);
        comment.TitleSlug.Should().Be("natra-2");
        comment.EpisodeNumber.Should().Be(1);
        comment.TimeSeconds.Should().Be(45);
        comment.Content.Should().Be("Phim đỉnh quá anh em ơi! 🔥");
        comment.Color.Should().Be("#f59e0b");
        comment.UserId.Should().BeNull();
        comment.AuthorName.Should().Be("ZMovieFan");
        comment.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void DanmakuComment_clamps_negative_timestamp_and_defaults_color()
    {
        var id = DanmakuCommentId.New();
        var comment = DanmakuComment.Create(
            id,
            "natra-2",
            1,
            -10,
            "Chào mừng",
            "",
            null,
            "",
            Now);

        comment.TimeSeconds.Should().Be(0);
        comment.Color.Should().Be("#ffffff");
        comment.AuthorName.Should().Be("Anonymous");
    }

    [Fact]
    public void DanmakuComment_has_private_parameterless_constructor_and_private_setters()
    {
        var constructor = typeof(DanmakuComment).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
        Assert.Empty(typeof(DanmakuComment).GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        var properties = typeof(DanmakuComment).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.All(properties, p =>
        {
            Assert.NotNull(p.SetMethod);
            Assert.True(p.SetMethod.IsPrivate);
        });
    }
}
