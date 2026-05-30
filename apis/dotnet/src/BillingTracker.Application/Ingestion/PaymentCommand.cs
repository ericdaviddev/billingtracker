namespace BillingTracker.Application.Ingestion;

public sealed record PaymentCommand
{
    public required Guid ClientId { get; init; }
    public required Guid SourceSystemId { get; init; }
    public required Guid LocationId { get; init; }
    public required Guid GuarantorId { get; init; }
    public required Guid DependentId { get; init; }
    public required string ExternalPaymentId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset PaymentDate { get; init; }
    public required string PaymentStatus { get; init; }
    public required DateTimeOffset SourceUpdatedAt { get; init; }
}
