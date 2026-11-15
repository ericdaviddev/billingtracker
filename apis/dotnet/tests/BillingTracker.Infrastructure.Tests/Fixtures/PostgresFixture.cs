using BillingTracker.Infrastructure.Data;
using Npgsql;
using Testcontainers.PostgreSql;

namespace BillingTracker.Infrastructure.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithUsername("billing")
        .WithPassword("billing")
        .WithDatabase("billingtracker")
        .Build();

    public DatabaseConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var cs = _container.GetConnectionString();
        ConnectionFactory = new DatabaseConnectionFactory(cs);

        foreach (var file in new[] { "00_create_schema.sql", "billing_create_tables.sql", "seed_data_inserts.sql" })
        {
            var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "sql", file));
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}