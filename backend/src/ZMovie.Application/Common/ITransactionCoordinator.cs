using System.Data;

namespace ZMovie.Application.Common;

public interface ITransactionCoordinator
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);
    Task ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
    Task<T> ExecuteInTransactionAsync<T>(IsolationLevel isolationLevel, Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
