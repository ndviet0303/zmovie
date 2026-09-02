using System.Reflection;
using FluentAssertions;
using Xunit;
using ZMovie.Domain.Engagement;

namespace ZMovie.Api.Tests.Domain.Engagement;

public sealed class GamificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 1, "Tân thủ", 100)]
    [InlineData(99, 1, "Tân thủ", 100)]
    [InlineData(100, 2, "Mọt phim", 500)]
    [InlineData(499, 2, "Mọt phim", 500)]
    [InlineData(500, 3, "Ghiền phim", 1500)]
    [InlineData(1500, 4, "Đại sư điện ảnh", 4000)]
    [InlineData(4000, 5, "Huyền thoại ZMovie", 10000)]
    [InlineData(12000, 6, "Chúa tể Rạp chiếu", 10000)]
    public void BilibiliLevelCalculator_calculates_correct_levels(int exp, int expectedLevel, string expectedTitle, int expectedNext)
    {
        var (level, title, current, next) = BilibiliLevelCalculator.Calculate(exp);
        level.Should().Be(expectedLevel);
        title.Should().Be(expectedTitle);
        current.Should().Be(exp);
        next.Should().Be(expectedNext);
    }

    [Fact]
    public void ShortClip_and_UserExpLedger_have_private_constructors_and_setters()
    {
        foreach (var type in new[] { typeof(ShortClip), typeof(UserExpLedger) })
        {
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
    }
}
