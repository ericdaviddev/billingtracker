namespace BillingTracker.Application.Ingestion;

public enum PaymentIngestionItemStatus
{
    Inserted,
    Updated,
    SkippedStale,
    SkippedUnchanged,
    Failed
}
