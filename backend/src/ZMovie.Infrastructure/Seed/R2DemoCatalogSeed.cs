using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Seed;

public static class R2DemoCatalogSeed
{
    public static async Task<int> SeedAsync(
        CatalogDbContext db,
        Func<string, string> getStreamUrl,
        TimeProvider? timeProvider = null,
        CancellationToken ct = default)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();

        var demoMovies = new (
            string Slug,
            string EnglishTitle,
            string VietnameseTitle,
            string EnglishSynopsis,
            string VietnameseSynopsis,
            string Genre,
            int Year,
            string Type,
            string PosterUrl,
            int RuntimeMinutes,
            string RelativeHlsPath,
            string RelativeSubPath,
            string TrailerUrl,
            string Actors,
            string Directors,
            string Country)[]
        {
            (
                "big-buck-bunny",
                "Big Buck Bunny",
                "Chú Thỏ Khổng Lồ",
                "A giant rabbit with a heart bigger than himself seeks gentle justice against bullying forest critters.",
                "Một chú thỏ khổng lồ tốt bụng quyết tâm trừng phạt thích đáng ba kẻ chuyên bắt nạt trong khu rừng để bảo vệ những người bạn nhỏ.",
                "Animation, Comedy",
                2024,
                "movie",
                "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=1200&q=80",
                10,
                "movies/big-buck-bunny/master.m3u8",
                "movies/big-buck-bunny/subtitles/vi.vtt",
                "https://www.youtube.com/watch?v=aqz-KE-bpKQ",
                "Big Buck Bunny, Frank, Rinky, Gimera",
                "Sacha Goedegebure",
                "Âu Mỹ"
            ),
            (
                "tears-of-steel",
                "Tears of Steel",
                "Nước Mắt Thép",
                "In a dystopian future, a group of scientists and warriors gather at the Oude Kerk in Amsterdam to stage a desperate alternate reality experiment.",
                "Trong một thế giới tương lai u tối bị robot thống trị, nhóm chiến binh và nhà khoa học tụ họp để thực hiện cuộc thí nghiệm thay đổi quá khứ.",
                "Science Fiction, Action",
                2025,
                "movie",
                "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=1200&q=80",
                12,
                "movies/tears-of-steel/master.m3u8",
                "movies/tears-of-steel/subtitles/vi.vtt",
                "https://www.youtube.com/watch?v=R6MlUcmOul8",
                "Derek de Lint, Sergio Hasselbaink, Rogier Schippers",
                "Ian Hubert",
                "Âu Mỹ"
            ),
            (
                "sintel",
                "Sintel",
                "Sintel: Hành Trình Tìm Rồng",
                "A lonely young woman searches the ends of the earth to rescue an orphaned baby dragon who became her only companion.",
                "Hành trình đầy xúc động của một cô gái trẻ đơn độc vượt qua bão tuyết và hiểm nguy để tìm lại chú rồng con mồ côi từng gắn bó với cô.",
                "Animation, Adventure, Fantasy",
                2024,
                "movie",
                "https://images.unsplash.com/photo-1514533450685-4493e01d1fdc?auto=format&fit=crop&w=1200&q=80",
                15,
                "movies/sintel/master.m3u8",
                "movies/sintel/subtitles/vi.vtt",
                "https://www.youtube.com/watch?v=eRsGyueVLvQ",
                "Halina Reijn, Thom Hoffman",
                "Colin Levy",
                "Âu Mỹ"
            )
        };

        var seededCount = 0;

        foreach (var item in demoMovies)
        {
            var slug = TitleSlug.Parse(item.Slug);
            var existing = await db.Titles.FirstOrDefaultAsync(x => x.Slug == slug, ct);
            var hlsUrl = getStreamUrl(item.RelativeHlsPath);
            var subUrl = getStreamUrl(item.RelativeSubPath);

            Title title;
            if (existing is null)
            {
                title = Title.Create(
                    TitleId.New(),
                    slug,
                    new LocalizedText(item.VietnameseTitle, item.EnglishTitle),
                    new LocalizedText(item.VietnameseSynopsis, item.EnglishSynopsis),
                    item.Genre,
                    ReleaseYear.FromInt(item.Year),
                    TitleType.Normalize(item.Type),
                    item.PosterUrl,
                    Runtime.FromMinutes(item.RuntimeMinutes),
                    featured: true,
                    now,
                    actors: item.Actors,
                    directors: item.Directors,
                    country: item.Country,
                    trailerUrl: item.TrailerUrl,
                    isR2Hosted: true);

                await db.Titles.AddAsync(title, ct);
            }
            else
            {
                existing.UpdateMetadata(
                    new LocalizedText(item.VietnameseTitle, item.EnglishTitle),
                    new LocalizedText(item.VietnameseSynopsis, item.EnglishSynopsis),
                    item.Genre,
                    ReleaseYear.FromInt(item.Year),
                    TitleType.Normalize(item.Type),
                    item.PosterUrl,
                    Runtime.FromMinutes(item.RuntimeMinutes),
                    featured: true,
                    now,
                    actors: item.Actors,
                    directors: item.Directors,
                    country: item.Country,
                    trailerUrl: item.TrailerUrl,
                    isR2Hosted: true);

                title = existing;
            }

            var existingEpisode = await db.Episodes.FirstOrDefaultAsync(x => x.TitleId == title.Id && x.Number == 1, ct);
            if (existingEpisode is null)
            {
                db.Episodes.Add(Episode.Create(EpisodeId.New(), title.Id, 1, "Bản Full HD (Cloudflare R2)", hlsUrl, subUrl));
            }
            else
            {
                existingEpisode.Update("Bản Full HD (Cloudflare R2)", hlsUrl, subUrl);
            }

            seededCount++;
        }

        await db.SaveChangesAsync(ct);
        return seededCount;
    }
}
