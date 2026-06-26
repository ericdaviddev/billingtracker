namespace BillingTracker.Application.Ingestion;

public sealed record PaymentIngestionResult
{
    public required Guid IngestionRunId { get; init; }
    public required PaymentIngestionRunStatus Status { get; init; }
    public required int RecordsReceived { get; init; }
    public required int RecordsInserted { get; init; }
    public required int RecordsUpdated { get; init; }
    public required int RecordsFailed { get; init; }
    public required IReadOnlyList<PaymentIngestionItemResult> ItemResults { get; init; }
}
