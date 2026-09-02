using ZMovie.Domain.Catalog;
using Xunit;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class CatalogDomainTests
{
    [Theory]
    [InlineData("vi", "Tiếng Việt")]
    [InlineData("en-US", "English")]
    public void Localized_title_uses_requested_locale(string locale, string expected)
    {
        var title = Title.Create(
            TitleId.New(),
            TitleSlug.Parse("test"),
            new LocalizedText("Tiếng Việt", "English"),
            new LocalizedText("Tiếng Việt", "English"),
            "Drama",
            ReleaseYear.FromInt(2026),
            TitleType.Movie,
            "https://example.test/poster.jpg",
            Runtime.FromMinutes(1),
            false,
            DateTimeOffset.UtcNow);

        Assert.Equal(expected, title.LocalizedTitle(locale));
    }
}
