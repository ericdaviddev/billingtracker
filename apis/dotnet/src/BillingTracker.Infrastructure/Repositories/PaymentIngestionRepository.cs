using System.Data;
using BillingTracker.Application.Ingestion;
using BillingTracker.Infrastructure.Data;
using Npgsql;

namespace BillingTracker.Infrastructure.Repositories;

public class PaymentIngestionRepository : IPaymentIngestionRepository
{
    private readonly DatabaseConnectionFactory _connectionFactory;

    public PaymentIngestionRepository(DatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid?> ResolveSourceSystemIdAsync(string sourceSystemName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT source_system_id 
            FROM billing.source_systems 
            WHERE source_system_name = @sourceSystemName";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sourceSystemName", sourceSystemName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as Guid?;
    }

    public async Task<Guid?> ResolveClientIdAsync(Guid sourceSystemId, string externalClientId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT client_id 
            FROM billing.client_source_mappings 
            WHERE source_system_id = @sourceSystemId 
              AND external_client_id = @externalClientId";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sourceSystemId", sourceSystemId);
        command.Parameters.AddWithValue("@externalClientId", externalClientId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as Guid?;
    }

    public async Task<Guid?> ResolveGuarantorIdAsync(Guid clientId, Guid sourceSystemId, string externalGuarantorId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT guarantor_id 
            FROM billing.guarantors 
            WHERE client_id = @clientId 
              AND source_system_id = @sourceSystemId 
              AND external_guarantor_id = @externalGuarantorId";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@sourceSystemId", sourceSystemId);
        command.Parameters.AddWithValue("@externalGuarantorId", externalGuarantorId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as Guid?;
    }

    public async Task<Guid?> ResolveDependentIdAsync(Guid clientId, Guid sourceSystemId, string externalDependentId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT dependent_id 
            FROM billing.dependents 
            WHERE client_id = @clientId 
              AND source_system_id = @sourceSystemId 
              AND external_dependent_id = @externalDependentId";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@sourceSystemId", sourceSystemId);
        command.Parameters.AddWithValue("@externalDependentId", externalDependentId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as Guid?;
    }

    public async Task<Guid?> ResolveLocationIdAsync(Guid clientId, Guid sourceSystemId, string externalLocationId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT location_id 
            FROM billing.location_source_mappings 
            WHERE client_id = @clientId 
              AND source_system_id = @sourceSystemId 
              AND external_location_id = @externalLocationId";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@sourceSystemId", sourceSystemId);
        command.Parameters.AddWithValue("@externalLocationId", externalLocationId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as Guid?;
    }

    public async Task<Guid> CreateIngestionRunAsync(Guid clientId, Guid sourceSystemId, string runType, int recordsReceived, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO billing.ingestion_runs (
                client_id, 
                source_system_id, 
                run_type, 
                status, 
                records_received
            )
            VALUES (
                @clientId, 
                @sourceSystemId, 
                @runType, 
                'running', 
                @recordsReceived
            )
            RETURNING ingestion_run_id";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@sourceSystemId", sourceSystemId);
        command.Parameters.AddWithValue("@runType", runType);
        command.Parameters.AddWithValue("@recordsReceived", recordsReceived);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (Guid)result!;
    }

    public async Task<PaymentUpsertResult> UpsertPaymentAsync(PaymentCommand command, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO billing.payments (
                client_id,
                location_id,
                source_system_id,
                guarantor_id,
                dependent_id,
                external_payment_id,
                payment_amount,
                payment_date,
                payment_status,
                source_updated_at
            )
            VALUES (
                @clientId,
                @locationId,
                @sourceSystemId,
                @guarantorId,
                @dependentId,
                @externalPaymentId,
                @amount,
                @paymentDate,
                @paymentStatus,
                @sourceUpdatedAt
            )
            ON CONFLICT (client_id, source_system_id, external_payment_id)
            DO UPDATE SET
                location_id = EXCLUDED.location_id,
                guarantor_id = EXCLUDED.guarantor_id,
                dependent_id = EXCLUDED.dependent_id,
                payment_amount = EXCLUDED.payment_amount,
                payment_date = EXCLUDED.payment_date,
                payment_status = EXCLUDED.payment_status,
                source_updated_at = EXCLUDED.source_updated_at,
                updated_at = NOW()
            WHERE billing.payments.source_updated_at <= EXCLUDED.source_updated_at
            RETURNING payment_id, 
                      CASE 
                          WHEN xmax = 0 THEN 'inserted'
                          WHEN xmax::text::bigint > 0 THEN 'updated'
                          ELSE 'skipped'
                      END as operation";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@clientId", command.ClientId);
        cmd.Parameters.AddWithValue("@locationId", command.LocationId);
        cmd.Parameters.AddWithValue("@sourceSystemId", command.SourceSystemId);
        cmd.Parameters.AddWithValue("@guarantorId", command.GuarantorId);
        cmd.Parameters.AddWithValue("@dependentId", command.DependentId);
        cmd.Parameters.AddWithValue("@externalPaymentId", command.ExternalPaymentId);
        cmd.Parameters.AddWithValue("@amount", command.Amount);
        cmd.Parameters.AddWithValue("@paymentDate", command.PaymentDate);
        cmd.Parameters.AddWithValue("@paymentStatus", command.PaymentStatus);
        cmd.Parameters.AddWithValue("@sourceUpdatedAt", command.SourceUpdatedAt);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            var paymentId = reader.GetGuid(0);
            var operation = reader.GetString(1);
            var status = FromDatabaseStatus(operation);

            return new PaymentUpsertResult
            {
                PaymentId = paymentId,
                Status = status
            };
        }

        return new PaymentUpsertResult
        {
            Status = PaymentIngestionItemStatus.SkippedStale,
            ErrorMessage = "Payment was not updated due to stale source_updated_at timestamp"
        };
    }

    public async Task InsertIngestionErrorAsync(
        Guid ingestionRunId, 
        string entityType, 
        string externalId, 
        string errorCode, 
        string errorMessage, 
        string payload, 
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO billing.ingestion_errors (
                ingestion_run_id,
                entity_type,
                external_id,
                error_code,
                error_message,
                payload
            )
            VALUES (
                @ingestionRunId,
                @entityType,
                @externalId,
                @errorCode,
                @errorMessage,
                @payload::jsonb
            )";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ingestionRunId", ingestionRunId);
        command.Parameters.AddWithValue("@entityType", entityType);
        command.Parameters.AddWithValue("@externalId", externalId);
        command.Parameters.AddWithValue("@errorCode", errorCode);
        command.Parameters.AddWithValue("@errorMessage", errorMessage);
        command.Parameters.AddWithValue("@payload", payload);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CompleteIngestionRunAsync(
        Guid ingestionRunId, 
        PaymentIngestionRunStatus status, 
        int recordsInserted, 
        int recordsUpdated, 
        int recordsFailed, 
        CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE billing.ingestion_runs
            SET status = @status,
                completed_at = NOW(),
                records_inserted = @recordsInserted,
                records_updated = @recordsUpdated,
                records_failed = @recordsFailed
            WHERE ingestion_run_id = @ingestionRunId";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var statusString = ToDatabaseStatus(status);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ingestionRunId", ingestionRunId);
        command.Parameters.AddWithValue("@status", statusString);
        command.Parameters.AddWithValue("@recordsInserted", recordsInserted);
        command.Parameters.AddWithValue("@recordsUpdated", recordsUpdated);
        command.Parameters.AddWithValue("@recordsFailed", recordsFailed);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ToDatabaseStatus(PaymentIngestionRunStatus status) =>
        status switch
        {
            PaymentIngestionRunStatus.Running => "running",
            PaymentIngestionRunStatus.Completed => "completed",
            PaymentIngestionRunStatus.Partial => "partial",
            PaymentIngestionRunStatus.Failed => "failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    private static PaymentIngestionItemStatus FromDatabaseStatus(string operation) =>
        operation switch
        {
            "inserted" => PaymentIngestionItemStatus.Inserted,
            "updated" => PaymentIngestionItemStatus.Updated,
            "skipped" => PaymentIngestionItemStatus.SkippedStale,
            _ => throw new ArgumentException($"Unknown database status: {operation}", nameof(operation))
        };
}
