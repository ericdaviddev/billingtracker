namespace BillingTracker.Application.Ingestion;

public interface IPaymentIngestionRepository
{
    Task<Guid?> ResolveSourceSystemIdAsync(string sourceSystemName, CancellationToken cancellationToken);
    
    Task<Guid?> ResolveClientIdAsync(Guid sourceSystemId, string externalClientId, CancellationToken cancellationToken);
    
    Task<Guid?> ResolveGuarantorIdAsync(Guid clientId, Guid sourceSystemId, string externalGuarantorId, CancellationToken cancellationToken);
    
    Task<Guid?> ResolveDependentIdAsync(Guid clientId, Guid sourceSystemId, string externalDependentId, CancellationToken cancellationToken);
    
    Task<Guid?> ResolveLocationIdAsync(Guid clientId, Guid sourceSystemId, string externalLocationId, CancellationToken cancellationToken);
    
    Task<Guid> CreateIngestionRunAsync(Guid clientId, Guid sourceSystemId, string runType, int recordsReceived, CancellationToken cancellationToken);
    
    Task<PaymentUpsertResult> UpsertPaymentAsync(PaymentCommand command, CancellationToken cancellationToken);
    
    Task InsertIngestionErrorAsync(Guid ingestionRunId, string entityType, string externalId, string errorCode, string errorMessage, string payload, CancellationToken cancellationToken);
    
    Task CompleteIngestionRunAsync(Guid ingestionRunId, PaymentIngestionRunStatus status, int recordsInserted, int recordsUpdated, int recordsFailed, CancellationToken cancellationToken);
}
