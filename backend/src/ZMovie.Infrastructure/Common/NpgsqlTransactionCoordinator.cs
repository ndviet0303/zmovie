using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ZMovie.Application.Common;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Infrastructure.Common;

public sealed class NpgsqlTransactionCoordinator(
    CatalogDbContext catalogDb,
    IdentityDbContext identityDb,
    EngagementDbContext engagementDb,
    AnalyticsDbContext analyticsDb,
    PersonalizationDbContext personalizationDb) : ITransactionCoordinator
{
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        ExecuteInTransactionAsync(null, action, ct);

    public async Task ExecuteInTransactionAsync(IsolationLevel? isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        if (!catalogDb.Database.IsNpgsql())
        {
            await action(ct);
            return;
        }

        await using var tx = isolationLevel.HasValue
            ? await catalogDb.Database.BeginTransactionAsync(isolationLevel.Value, ct)
            : await catalogDb.Database.BeginTransactionAsync(ct);

        var dbTx = tx.GetDbTransaction();

        EnlistAll(dbTx);
        try
        {
            await action(ct);
            await tx.CommitAsync(ct);
        }
        finally
        {
            UnenlistAll();
        }
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        ExecuteInTransactionAsync(null, action, ct);

    public async Task<T> ExecuteInTransactionAsync<T>(IsolationLevel? isolationLevel, Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        if (!catalogDb.Database.IsNpgsql())
        {
            return await action(ct);
        }

        await using var tx = isolationLevel.HasValue
            ? await catalogDb.Database.BeginTransactionAsync(isolationLevel.Value, ct)
            : await catalogDb.Database.BeginTransactionAsync(ct);

        var dbTx = tx.GetDbTransaction();

        EnlistAll(dbTx);
        try
        {
            var result = await action(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        finally
        {
            UnenlistAll();
        }
    }

    Task ITransactionCoordinator.ExecuteInTransactionAsync(IsolationLevel isolationLevel, Func<CancellationToken, Task> action, CancellationToken ct) =>
        ExecuteInTransactionAsync(isolationLevel, action, ct);

    Task<T> ITransactionCoordinator.ExecuteInTransactionAsync<T>(IsolationLevel isolationLevel, Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        ExecuteInTransactionAsync(isolationLevel, action, ct);

    private void EnlistAll(System.Data.Common.DbTransaction dbTx)
    {
        var connection = catalogDb.Database.GetDbConnection();

        if (identityDb.Database.IsNpgsql())
        {
            identityDb.Database.SetDbConnection(connection);
            identityDb.Database.UseTransaction(dbTx);
        }
        if (engagementDb.Database.IsNpgsql())
        {
            engagementDb.Database.SetDbConnection(connection);
            engagementDb.Database.UseTransaction(dbTx);
        }
        if (analyticsDb.Database.IsNpgsql())
        {
            analyticsDb.Database.SetDbConnection(connection);
            analyticsDb.Database.UseTransaction(dbTx);
        }
        if (personalizationDb.Database.IsNpgsql())
        {
            personalizationDb.Database.SetDbConnection(connection);
            personalizationDb.Database.UseTransaction(dbTx);
        }
    }

    private void UnenlistAll()
    {
        if (identityDb.Database.IsNpgsql()) identityDb.Database.UseTransaction(null);
        if (engagementDb.Database.IsNpgsql()) engagementDb.Database.UseTransaction(null);
        if (analyticsDb.Database.IsNpgsql()) analyticsDb.Database.UseTransaction(null);
        if (personalizationDb.Database.IsNpgsql()) personalizationDb.Database.UseTransaction(null);
    }
}
