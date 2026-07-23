using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Seed;

public static class CatalogSeed
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        var natra = await EnsureTitle(db, Create("natra-2-ma-dong-nao-hai", "Ne Zha 2: Demon Child Rages the Sea", "Natra 2: Ma Đồng Náo Hải", "Ne Zha faces a new trial across the sea.", "Natra đối diện thử thách mới giữa biển khơi.", "Animation", 2025, "movie", "https://upload.wikimedia.org/wikipedia/en/b/b6/Ne_Zha_2_poster.jpg", 144, true), ct);
        var mushoku = await EnsureTitle(db, Create("that-nghiep-chuyen-sinh-phan-3", "Mushoku Tensei Season 3", "Thất nghiệp chuyển sinh phần 3", "Rudeus continues his journey in a new chapter.", "Rudeus tiếp tục hành trình của mình trong chương mới.", "Animation", 2026, "series", "https://static.animecorner.me/2026/05/1779187746-6b497063174d0afdd8395904b6919d82.jpg", 24, true), ct);
        var translateLove = await EnsureTitle(db, Create("tieng-yeu-nay-anh-dich-duoc-khong", "Can This Love Be Translated?", "Tiếng yêu này, anh dịch được không?", "A celebrity and her interpreter struggle to make sense of their feelings while traveling the world to film a television show.", "Cảm xúc của một ngôi sao và phiên dịch viên của cô dễ lạc mất ý nghĩa khi họ cùng đi khắp thế giới để quay một chương trình truyền hình. Liệu tình yêu có tự tìm được ngôn ngữ riêng?", "Romance", 2026, "series", "https://occ-0-325-395.1.nflxso.net/dnm/api/v6/6AYY37jfdO6hpXcMjf9Yu5cnmO0/AAAABb3rnuwKYw2K1nEKI24J2BZCyTcttUmJkw9EXzIDUyH7wAqOU_WgR6nfWzXIaZOQADepqvSIZ_r0cu6ruYLasOwCCJrOf_jDLCai.jpg?r=605", 60, false), ct);
        var heavyKnight = await EnsureTitle(db, Create("trong-giap-hiep-si-chuyen-sinh", "The Exiled Heavy Knight Knows How to Game the System", "Trọng Giáp Hiệp Sĩ Chuyển Sinh Bị Lưu Đày Trở Nên Vô Địch Nhờ Kiến Thức Về Game", "Born into a famous swordsman family, Elymas is disowned after awakening as a supposedly defective Heavy Knight. Memories of a past life reveal that this world is the VR game he once mastered, giving him the knowledge to rewrite his fate.", "Sinh ra trong gia tộc kiếm sĩ danh giá, Elymas bị ruồng bỏ khi thức tỉnh thành Trọng Giáp Hiệp Sĩ bị cho là vô dụng. Ký ức kiếp trước cho cậu biết đây chính là thế giới VR game từng chinh phục, giúp cậu viết lại số phận.", "Animation", 2026, "series", "https://sh-anime.shochiku.co.jp/jukishi-anime/img/ju_ogp_1.jpg", 24, false), ct);
        var mushokuPart1 = await EnsureTitle(db, Create("that-nghiep-chuyen-sinh-phan-1", "Mushoku Tensei: Jobless Reincarnation", "Thất Nghiệp Chuyển Sinh - Phần 1", "A man is reborn in a magical world and resolves to live a life without regrets.", "Một người đàn ông tái sinh ở thế giới phép thuật và quyết tâm sống cuộc đời không hối tiếc.", "Animation", 2021, "series", "https://images.unsplash.com/photo-1518709594023-6eab9bab7b23?auto=format&fit=crop&w=1200&q=80", 24, false), ct);
        var mushokuPart2 = await EnsureTitle(db, Create("that-nghiep-chuyen-sinh-phan-2", "Mushoku Tensei: Jobless Reincarnation Season 2 Part 2", "Thất Nghiệp Chuyển Sinh - Phần 2", "Rudeus continues his adventure as new bonds and challenges shape his path.", "Rudeus tiếp tục cuộc phiêu lưu khi những mối gắn kết và thử thách mới định hình con đường của cậu.", "Animation", 2024, "series", "https://images.unsplash.com/photo-1518709594023-6eab9bab7b23?auto=format&fit=crop&w=1200&q=80", 24, false), ct);
        var teachYouLesson = await EnsureTitle(db, Create("bai-hoc-dang-doi", "Teach You a Lesson", "Bài Học Đáng Đời", "When respect collapses in schools, unconventional inspectors arrive to set things right with sharp, no-nonsense lessons.", "Khi sự tôn trọng trong trường học sụp đổ, những thanh tra không theo lối mòn xuất hiện để lập lại trật tự bằng những bài học thẳng thắn, không có trong sách giáo khoa.", "Action & Drama", 2026, "series", "https://occ-0-325-395.1.nflxso.net/dnm/api/v6/6AYY37jfdO6hpXcMjf9Yu5cnmO0/AAAABQ3AcAl3rz1Wl2YD0IA2m2FktFVwjArejUBWmfEpDn2RKx5RQHQFGvY3ugc-f_cTJzWhw3tYban2fnpPSVbSE_SbRBnNVXGZUz8G.jpg?r=38b", 52, false), ct);

        await EnsureEpisodes(db, natra, ["https://vip.opstream90.com/20250731/9896_dd1970fb/index.m3u8"], ct);
        await EnsureEpisodes(db, mushoku, ["https://vip.opstream90.com/20260705/36184_705dd9f9/index.m3u8", "https://vip.opstream90.com/20260705/36185_f0260654/index.m3u8", "https://vip.opstream10.com/20260720/34407_ee79d5e1/index.m3u8", "https://vip.opstream10.com/20260720/34408_8f8f6380/index.m3u8"], ct);
        await EnsureEpisodes(db, translateLove, ["https://vip.opstream90.com/20260116/22574_4eb9407d/index.m3u8", "https://vip.opstream90.com/20260116/22575_026947ba/index.m3u8", "https://vip.opstream90.com/20260116/22576_a5311ea2/index.m3u8", "https://vip.opstream90.com/20260116/22577_3d5ac5d0/index.m3u8", "https://vip.opstream90.com/20260116/22578_8e6a4217/index.m3u8", "https://vip.opstream90.com/20260116/22579_599b61ce/index.m3u8", "https://vip.opstream90.com/20260116/22580_972a8c3b/index.m3u8", "https://vip.opstream90.com/20260116/22581_2393d7a6/index.m3u8", "https://vip.opstream90.com/20260116/22582_3e30c0a4/index.m3u8", "https://vip.opstream90.com/20260116/22583_94d231f1/index.m3u8", "https://vip.opstream90.com/20260116/22584_266e3c74/index.m3u8", "https://vip.opstream90.com/20260116/22585_b6d7a951/index.m3u8"], ct);
        await EnsureEpisodes(db, heavyKnight, ["https://vip.opstream10.com/20260718/34307_57990ea7/index.m3u8", "https://vip.opstream10.com/20260718/34308_c4f7cfb1/index.m3u8", "https://vip.opstream10.com/20260718/34309_b05ed73e/index.m3u8"], ct);
        await EnsureEpisodes(db, mushokuPart1, ["https://vip.opstream12.com/20220608/17621_8a3cdb75/index.m3u8", "https://vip.opstream12.com/20220608/17622_61dde29a/index.m3u8", "https://vip.opstream12.com/20220608/17623_f90e286a/index.m3u8", "https://vip.opstream12.com/20220608/17624_e327b351/index.m3u8", "https://vip.opstream12.com/20220608/17625_6ed990aa/index.m3u8", "https://vip.opstream12.com/20220608/17626_26dc9619/index.m3u8", "https://vip.opstream12.com/20220608/17627_b475f9dc/index.m3u8", "https://vip.opstream12.com/20220608/17628_6b161e6d/index.m3u8", "https://vip.opstream12.com/20220608/17629_d0c81f27/index.m3u8", "https://vip.opstream12.com/20220608/17630_973a3433/index.m3u8", "https://vip.opstream12.com/20220608/17631_5b108cc8/index.m3u8", "https://vip.opstream12.com/20220608/17632_061ee642/index.m3u8", "https://vip.opstream12.com/20220608/17633_3adfe586/index.m3u8", "https://vip.opstream12.com/20220608/17634_e1d88079/index.m3u8", "https://vip.opstream12.com/20220608/17635_f9a5aeb3/index.m3u8", "https://vip.opstream12.com/20220608/17636_3742deb6/index.m3u8", "https://vip.opstream12.com/20220608/17637_e45ac783/index.m3u8", "https://vip.opstream12.com/20220608/17638_800f59e3/index.m3u8", "https://vip.opstream12.com/20220608/17639_c0d3fb26/index.m3u8", "https://vip.opstream12.com/20220608/17640_d32e138f/index.m3u8", "https://vip.opstream12.com/20220608/17641_2f8a941c/index.m3u8", "https://vip.opstream12.com/20220608/17642_f03dbbe0/index.m3u8", "https://vip.opstream12.com/20220608/17643_a25cfcae/index.m3u8", "https://vip.opstream12.com/20220608/17644_6f09c049/index.m3u8"], ct);
        await EnsureEpisodes(db, teachYouLesson, ["https://vip.opstream90.com/20260605/33879_0038c2d2/index.m3u8", "https://vip.opstream90.com/20260605/33880_df8db2b0/index.m3u8", "https://vip.opstream90.com/20260605/33881_e144a462/index.m3u8", "https://vip.opstream90.com/20260605/33882_eff4cedd/index.m3u8", "https://vip.opstream90.com/20260605/33883_cfe12d07/index.m3u8", "https://vip.opstream90.com/20260605/33884_5549f6da/index.m3u8", "https://vip.opstream90.com/20260605/33885_a5220ac6/index.m3u8", "https://vip.opstream90.com/20260605/33886_8ace2e13/index.m3u8", "https://vip.opstream90.com/20260605/33887_62da0d73/index.m3u8", "https://vip.opstream90.com/20260605/33888_a9b87f59/index.m3u8"], ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task<CatalogTitle> EnsureTitle(CatalogDbContext db, CatalogTitle candidate, CancellationToken ct)
    {
        var existing = await db.Titles.FirstOrDefaultAsync(x => x.Slug == candidate.Slug, ct);
        if (existing is null) return (await db.Titles.AddAsync(candidate, ct)).Entity;
        existing.EnglishTitle = candidate.EnglishTitle;
        existing.VietnameseTitle = candidate.VietnameseTitle;
        existing.EnglishSynopsis = candidate.EnglishSynopsis;
        existing.VietnameseSynopsis = candidate.VietnameseSynopsis;
        existing.Genre = candidate.Genre;
        existing.Year = candidate.Year;
        existing.Type = candidate.Type;
        existing.PosterUrl = candidate.PosterUrl;
        existing.RuntimeMinutes = candidate.RuntimeMinutes;
        existing.Featured = candidate.Featured;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        return existing;
    }

    private static async Task EnsureEpisodes(CatalogDbContext db, CatalogTitle title, IReadOnlyList<string> urls, CancellationToken ct)
    {
        var existingNumbers = await db.Episodes.Where(x => x.TitleId == title.Id).Select(x => x.Number).ToListAsync(ct);
        for (var index = 0; index < urls.Count; index++)
        {
            var number = index + 1;
            if (!existingNumbers.Contains(number)) db.Episodes.Add(new CatalogEpisode { TitleId = title.Id, Number = number, Name = $"Episode {number}", HlsUrl = urls[index] });
        }
    }

    private static CatalogTitle Create(string slug, string englishTitle, string vietnameseTitle, string englishSynopsis, string vietnameseSynopsis, string genre, int year, string type, string posterUrl, int runtimeMinutes, bool featured) => new()
    { Slug = slug, EnglishTitle = englishTitle, VietnameseTitle = vietnameseTitle, EnglishSynopsis = englishSynopsis, VietnameseSynopsis = vietnameseSynopsis, Genre = genre, Year = year, Type = type, PosterUrl = posterUrl, RuntimeMinutes = runtimeMinutes, Featured = featured };
}
