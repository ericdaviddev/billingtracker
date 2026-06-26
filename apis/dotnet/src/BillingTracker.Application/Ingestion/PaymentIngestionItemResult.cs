namespace BillingTracker.Application.Ingestion;

public sealed record PaymentIngestionItemResult
{
    public required string ExternalPaymentId { get; init; }
    public required PaymentIngestionItemStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? PaymentId { get; init; }
}
