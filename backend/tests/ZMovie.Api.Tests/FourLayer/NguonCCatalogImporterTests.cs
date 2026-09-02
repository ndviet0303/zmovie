using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class NguonCCatalogImporterTests
{
    [Fact]
    public async Task Imports_nguonc_pages_and_maps_metadata_and_episodes()
    {
        using var database = new TestDatabase();
        var listJson = JsonSerializer.Serialize(new
        {
            status = "success",
            paginate = new
            {
                current_page = 1,
                total_page = 1,
                total_items = 1,
                items_per_page = 20
            },
            items = new[]
            {
                new
                {
                    name = "Tây Du Ký 2024",
                    slug = "tay-du-ky-2024",
                    original_name = "Journey to the West",
                    thumb_url = "https://cdn.example.com/thumb.jpg",
                    poster_url = "https://cdn.example.com/poster.jpg",
                    description = "<p>Phim hành trình <b>hấp dẫn</b></p>",
                    time = "45 phút / tập",
                    director = "Đạo diễn A",
                    casts = "Diễn viên B, Diễn viên C",
                    category = new Dictionary<string, object>
                    {
                        ["1"] = new { group = new { id = "1", name = "Định dạng" }, list = new[] { new { id = "phim-bo", name = "Phim bộ" } } },
                        ["2"] = new { group = new { id = "2", name = "Thể loại" }, list = new[] { new { id = "hanh-dong", name = "Hành Động" } } },
                        ["3"] = new { group = new { id = "3", name = "Quốc gia" }, list = new[] { new { id = "trung-quoc", name = "Trung Quốc" } } },
                        ["4"] = new { group = new { id = "4", name = "Năm" }, list = new[] { new { id = "2024", name = "2024" } } }
                    }
                }
            }
        });

        var detailJson = JsonSerializer.Serialize(new
        {
            status = "success",
            movie = new
            {
                id = "12345",
                name = "Tây Du Ký 2024",
                slug = "tay-du-ky-2024",
                episodes = new[]
                {
                    new
                    {
                        server_name = "Vietsub",
                        items = new[]
                        {
                            new
                            {
                                name = "Tập 1",
                                slug = "tap-1",
                                embed = "https://embed.streamc.xyz/v/1",
                                m3u8 = "https://hls.streamc.xyz/1/master.m3u8"
                            }
                        }
                    }
                }
            }
        });

        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, listJson)
            .Enqueue(HttpStatusCode.OK, detailJson);

        using var http = new HttpClient(handler);
        var reports = new List<string>();

        var result = await NguonCCatalogImporter.ImportAsync(
            database.Db,
            http,
            new NguonCCatalogImportOptions(1, 1, true, TimeSpan.Zero),
            reports.Add,
            CancellationToken.None);

        result.Should().Be(new NguonCCatalogImportResult(1, 1, 1, 1));
        database.Db.Titles.Should().HaveCount(1);
        var title = await database.Db.Titles.FirstAsync();
        title.Slug.Value.Should().Be("tay-du-ky-2024");
        title.VietnameseTitle.Should().Be("Tây Du Ký 2024");
        title.EnglishTitle.Should().Be("Journey to the West");
        title.Genre.Should().Be("Hành Động");
        title.Country.Should().Be("Trung Quốc");
        title.Directors.Should().Be("Đạo diễn A");
        title.Actors.Should().Be("Diễn viên B, Diễn viên C");
        title.Type.Value.Should().Be("series");

        database.Db.Episodes.Should().HaveCount(1);
        var episode = await database.Db.Episodes.FirstAsync();
        episode.Number.Should().Be(1);
        episode.HlsUrl.Should().Be("https://hls.streamc.xyz/1/master.m3u8");
    }
}
