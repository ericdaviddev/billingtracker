namespace BillingTracker.Application.Ingestion;

public sealed record PaymentIngestionRequest
{
    public required string ClientExternalId { get; init; }
    public required string SourceSystem { get; init; }
    public required IReadOnlyList<PaymentIngestionItem> Payments { get; init; }
}
