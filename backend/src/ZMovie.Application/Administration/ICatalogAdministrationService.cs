namespace ZMovie.Application.Administration;

public interface ICatalogAdministrationService
{
    Task<AdminTitleDetail?> UpdateTitleAsync(string slug, AdminTitleEdit edit, CancellationToken ct);
    Task<AdminTitleDetail?> SetTitleFeaturedAsync(string slug, bool featured, CancellationToken ct);
    Task<AdminGenreSummary?> CreateGenreAsync(string slug, string name, CancellationToken ct);
    Task<AdminGenreSummary?> UpdateGenreAsync(Guid id, string name, CancellationToken ct);
    Task<bool> DeleteGenreAsync(Guid id, CancellationToken ct);
}
