using System.Reflection;
using FluentAssertions;
using ZMovie.Domain.Catalog;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Catalog;

public sealed class CatalogDomainTests
{
    private static readonly DateTimeOffset InitialTime = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TitleSlug_validates_pattern_and_length()
    {
        TitleSlug.TryCreate("valid-slug-123", out var slug).Should().BeTrue();
        slug.Value.Should().Be("valid-slug-123");

        TitleSlug.TryCreate("INVALID_SLUG", out _).Should().BeFalse();
        TitleSlug.TryCreate("slug--double-dash", out _).Should().BeFalse();
        TitleSlug.TryCreate("-leading-dash", out _).Should().BeFalse();
        TitleSlug.TryCreate("trailing-dash-", out _).Should().BeFalse();
        TitleSlug.TryCreate(null, out _).Should().BeFalse();
        TitleSlug.TryCreate("", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("movie", true, false)]
    [InlineData("series", false, true)]
    [InlineData("  Movie  ", true, false)]
    [InlineData("SERIES", false, true)]
    public void TitleType_parses_known_types(string input, bool expectedMovie, bool expectedSeries)
    {
        TitleType.TryCreate(input, out var type).Should().BeTrue();
        type.IsMovie.Should().Be(expectedMovie);
        type.IsSeries.Should().Be(expectedSeries);
    }

    [Theory]
    [InlineData(null, "movie")]
    [InlineData("invalid", "movie")]
    [InlineData("series", "series")]
    public void TitleType_normalizes_unknown_values(string? input, string expected)
    {
        TitleType.Normalize(input).Value.Should().Be(expected);
    }

    [Fact]
    public void LocalizedText_returns_localized_values()
    {
        var text = new LocalizedText("Xin chào", "Hello");

        text.Localize("vi").Should().Be("Xin chào");
        text.Localize("en").Should().Be("Hello");
        text.Localize("en-US").Should().Be("Hello");
        text.Localize(null).Should().Be("Xin chào");
    }

    [Theory]
    [InlineData(2026, true, 2026)]
    [InlineData(1888, true, 1888)]
    [InlineData(2100, true, 2100)]
    [InlineData(0, false, 0)]
    [InlineData(1800, false, 0)]
    [InlineData(2200, false, 0)]
    public void ReleaseYear_handles_known_and_unknown_years(int input, bool expectedKnown, int expectedValue)
    {
        var year = ReleaseYear.FromInt(input);
        year.IsKnown.Should().Be(expectedKnown);
        year.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Runtime_validates_non_negative_minutes()
    {
        Runtime.TryCreate(120, out var valid).Should().BeTrue();
        valid.Minutes.Should().Be(120);

        Runtime.TryCreate(-1, out _).Should().BeFalse();
        Runtime.FromMinutes(-10).Minutes.Should().Be(0);
    }

    [Fact]
    public void Title_create_and_update_maintains_invariants()
    {
        var id = TitleId.New();
        var slug = TitleSlug.Parse("movie-one");
        var titleName = new LocalizedText("Phim Một", "Movie One");
        var synopsis = new LocalizedText("Mô tả một", "Synopsis one");

        var title = Title.Create(
            id,
            slug,
            titleName,
            synopsis,
            "Drama, Action",
            ReleaseYear.FromInt(2026),
            TitleType.Movie,
            "https://poster.test/1.jpg",
            Runtime.FromMinutes(110),
            false,
            InitialTime);

        title.Id.Should().Be(id);
        title.Slug.Should().Be(slug);
        title.TitleName.Should().Be(titleName);
        title.Synopsis.Should().Be(synopsis);
        title.Genre.Should().Be("Drama, Action");
        title.Year.Value.Should().Be(2026);
        title.Type.Should().Be(TitleType.Movie);
        title.PosterUrl.Should().Be("https://poster.test/1.jpg");
        title.Runtime.Minutes.Should().Be(110);
        title.Featured.Should().BeFalse();
        title.CreatedAt.Should().Be(InitialTime);
        title.UpdatedAt.Should().Be(InitialTime);

        var updateTime = InitialTime.AddDays(1);
        title.UpdateMetadata(
            new LocalizedText("Phim Một Đổi", "Movie One Updated"),
            synopsis,
            "Drama",
            ReleaseYear.FromInt(2025),
            TitleType.Series,
            "https://poster.test/updated.jpg",
            Runtime.FromMinutes(50),
            true,
            updateTime);

        title.LocalizedTitle("vi").Should().Be("Phim Một Đổi");
        title.LocalizedTitle("en").Should().Be("Movie One Updated");
        title.Genre.Should().Be("Drama");
        title.Type.Should().Be(TitleType.Series);
        title.Featured.Should().BeTrue();
        title.UpdatedAt.Should().Be(updateTime);

        title.SetFeatured(false, updateTime.AddHours(2));
        title.Featured.Should().BeFalse();
        title.UpdatedAt.Should().Be(updateTime.AddHours(2));
    }

    [Fact]
    public void Episode_create_and_update_maintains_fields()
    {
        var id = EpisodeId.New();
        var titleId = TitleId.New();

        var episode = Episode.Create(id, titleId, 1, "Tập 1", "https://hls.test/1.m3u8");

        episode.Id.Should().Be(id);
        episode.TitleId.Should().Be(titleId);
        episode.Number.Should().Be(1);
        episode.Name.Should().Be("Tập 1");
        episode.HlsUrl.Should().Be("https://hls.test/1.m3u8");

        episode.Update("Tập 1 (HD)", "https://hls.test/1-hd.m3u8");
        episode.Name.Should().Be("Tập 1 (HD)");
        episode.HlsUrl.Should().Be("https://hls.test/1-hd.m3u8");
    }

    [Fact]
    public void Genre_create_and_rename_maintains_fields()
    {
        var id = GenreId.New();

        var genre = Genre.Create(id, "hanh-dong", "Hành Động", InitialTime);

        genre.Id.Should().Be(id);
        genre.Slug.Should().Be("hanh-dong");
        genre.Name.Should().Be("Hành Động");
        genre.UpdatedAt.Should().Be(InitialTime);

        var updateTime = InitialTime.AddDays(1);
        genre.Rename("Hành Động & Phiêu Lưu", updateTime);
        genre.Name.Should().Be("Hành Động & Phiêu Lưu");
        genre.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void TitleGenreAssignment_creates_association()
    {
        var titleId = TitleId.New();
        var genreId = GenreId.New();

        var assignment = TitleGenreAssignment.Create(titleId, genreId, InitialTime);

        assignment.TitleId.Should().Be(titleId);
        assignment.GenreId.Should().Be(genreId);
        assignment.AssignedAt.Should().Be(InitialTime);
    }

    [Fact]
    public void EpisodeStreamSource_creation_and_mutation()
    {
        var sourceId = EpisodeSourceId.New();
        var episodeId = EpisodeId.New();
        var source = EpisodeStreamSource.Create(
            sourceId,
            episodeId,
            "R2-VIP",
            "https://cdn.example.com/stream.m3u8",
            "HLS",
            1,
            true,
            "https://cdn.example.com/sub.vtt",
            "vi-sub");

        source.Id.Should().Be(sourceId);
        source.EpisodeId.Should().Be(episodeId);
        source.Provider.Should().Be("R2-VIP");
        source.Url.Should().Be("https://cdn.example.com/stream.m3u8");
        source.Format.Should().Be("hls");
        source.Priority.Should().Be(1);
        source.IsActive.Should().BeTrue();
        source.SubtitleUrl.Should().Be("https://cdn.example.com/sub.vtt");
        source.AudioTrack.Should().Be("vi-sub");

        source.UpdateStatus(false);
        source.IsActive.Should().BeFalse();

        source.UpdateDetails("https://cdn.example.com/stream2.m3u8", "embed", 2, "https://cdn.example.com/sub2.vtt", "vi-dub");
        source.Url.Should().Be("https://cdn.example.com/stream2.m3u8");
        source.Format.Should().Be("embed");
        source.Priority.Should().Be(2);
        source.SubtitleUrl.Should().Be("https://cdn.example.com/sub2.vtt");
        source.AudioTrack.Should().Be("vi-dub");
    }

    [Fact]
    public void PlaybackMilestones_detects_intro_and_outro()
    {
        var valid = new PlaybackMilestones(90, 180, 1400, 1500);
        valid.HasIntro.Should().BeTrue();
        valid.HasOutro.Should().BeTrue();

        var none = PlaybackMilestones.None;
        none.HasIntro.Should().BeFalse();
        none.HasOutro.Should().BeFalse();

        var invalid = new PlaybackMilestones(180, 90, null, null);
        invalid.HasIntro.Should().BeFalse();
    }

    [Fact]
    public void Episode_manages_sources_and_milestones()
    {
        var episodeId = EpisodeId.New();
        var titleId = TitleId.New();
        var episode = Episode.Create(episodeId, titleId, 1, "Tập 1", "https://cdn.example.com/ep1.m3u8");

        episode.Sources.Should().HaveCount(1);
        episode.Sources.First().Url.Should().Be("https://cdn.example.com/ep1.m3u8");
        episode.Milestones.Should().Be(PlaybackMilestones.None);

        var milestones = new PlaybackMilestones(60, 120, null, null);
        episode.SetMilestones(milestones);
        episode.Milestones.Should().Be(milestones);

        var backupSource = EpisodeStreamSource.Create(EpisodeSourceId.New(), episodeId, "OPhim", "https://ophim.example.com/ep1.m3u8", "hls", 2);
        episode.AddSource(backupSource);
        episode.Sources.Should().HaveCount(2);

        episode.RemoveSource(backupSource.Id);
        episode.Sources.Should().HaveCount(1);
    }

    [Fact]
    public void Aggregate_roots_have_private_parameterless_constructors_and_private_setters()
    {
        foreach (var type in new[] { typeof(Title), typeof(Episode), typeof(Genre), typeof(TitleGenreAssignment), typeof(EpisodeStreamSource) })
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

