using Npgsql;

namespace BillingTracker.Infrastructure.Data;

public class DatabaseConnectionFactory
{
    private readonly string _connectionString;

    public DatabaseConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
