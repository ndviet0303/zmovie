namespace ZMovie.Application.Administration;

public interface IAdminTitleDeletionCoordinator
{
    Task<bool> DeleteTitleAsync(string slug, CancellationToken ct);
}
