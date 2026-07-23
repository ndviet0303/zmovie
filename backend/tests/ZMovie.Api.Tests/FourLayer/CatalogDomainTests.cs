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
        var title = new CatalogTitle
        {
            Slug = "test", EnglishTitle = "English", VietnameseTitle = "Tiếng Việt",
            EnglishSynopsis = "English", VietnameseSynopsis = "Tiếng Việt", Genre = "Drama",
            Year = 2026, Type = "movie", PosterUrl = "https://example.test/poster.jpg", RuntimeMinutes = 1
        };

        Assert.Equal(expected, title.LocalizedTitle(locale));
    }
}
