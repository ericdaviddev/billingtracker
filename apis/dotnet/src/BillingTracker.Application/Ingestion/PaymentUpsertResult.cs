namespace BillingTracker.Application.Ingestion;

public sealed record PaymentUpsertResult
{
    public Guid? PaymentId { get; init; }
    public required PaymentIngestionItemStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
}
