namespace BillingTracker.Application.Ingestion;

public sealed record PaymentIngestionItem
{
    public required string ExternalPaymentId { get; init; }
    public required string ExternalGuarantorId { get; init; }
    public required string ExternalDependentId { get; init; }
    public required string ExternalLocationId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset PaymentDate { get; init; }
    public required string PaymentStatus { get; init; }
    public required DateTimeOffset SourceUpdatedAt { get; init; }
}
