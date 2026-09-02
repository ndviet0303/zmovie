using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace ZMovie.Api.Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration";
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _adminConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("ZMOVIE_TEST_POSTGRES") is { Length: > 0 } externalConnection)
        {
            _adminConnectionString = AdminConnectionString(externalConnection);
            await VerifyConnectionAsync();
            return;
        }

        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await _container.StartAsync();
        _adminConnectionString = AdminConnectionString(_container.GetConnectionString());
    }

    public Task DisposeAsync() => _container is null
        ? Task.CompletedTask
        : _container.DisposeAsync().AsTask();

    public async Task<PostgreSqlDatabaseLease> CreateDatabaseAsync()
    {
        var databaseName = $"zmovie_test_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = databaseName,
            Pooling = true,
        };
        return new PostgreSqlDatabaseLease(this, databaseName, builder.ConnectionString);
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task VerifyConnectionAsync()
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_adminConnectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                _ = await command.ExecuteScalarAsync();
                return;
            }
            catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
            {
                lastError = exception;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new InvalidOperationException("The PostgreSQL integration-test server did not become ready within 30 seconds.", lastError);
    }

    private static string AdminConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres",
            Pooling = false,
            Timeout = 3,
            CommandTimeout = 5,
        };
        return builder.ConnectionString;
    }

    public sealed class PostgreSqlDatabaseLease(
        PostgreSqlFixture owner,
        string databaseName,
        string connectionString) : IAsyncDisposable
    {
        public string ConnectionString { get; } = connectionString;

        public ValueTask DisposeAsync() => new(owner.DropDatabaseAsync(databaseName));
    }
}
