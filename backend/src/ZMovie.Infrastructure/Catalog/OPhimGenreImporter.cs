using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public static class OPhimGenreImporter
{
    public static async Task<int> ImportAsync(CatalogDbContext db, HttpClient http, CancellationToken ct) =>
        await ImportAsync(db, http, TimeProvider.System, ct);

    public static async Task<int> ImportAsync(CatalogDbContext db, HttpClient http, TimeProvider timeProvider, CancellationToken ct)
    {
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var response = await http.GetFromJsonAsync<OPhimGenresResponse>("https://ophim1.com/v1/api/the-loai", ct)
            ?? throw new InvalidOperationException("OPhim returned no payload.");
        if (!string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"OPhim request failed: {response.Message}");

        var now = timeProvider.GetUtcNow();
        var imported = 0;
        foreach (var source in response.Data.Items.Where(x => !string.IsNullOrWhiteSpace(x.Slug) && !string.IsNullOrWhiteSpace(x.Name)))
        {
            var normalizedSlug = source.Slug.Trim().ToLowerInvariant();
            var genre = await db.Genres.SingleOrDefaultAsync(x => x.Slug == normalizedSlug, ct);
            if (genre is null)
            {
                db.Genres.Add(Genre.Create(GenreId.New(), normalizedSlug, source.Name, now));
            }
            else
            {
                genre.Rename(source.Name, now);
            }
            imported++;
        }
        await db.SaveChangesAsync(ct);
        return imported;
    }

    private sealed record OPhimGenresResponse(string Status, string? Message, OPhimGenresData Data);
    private sealed record OPhimGenresData(IReadOnlyList<OPhimGenre> Items);
    private sealed record OPhimGenre(string Name, string Slug);
}
