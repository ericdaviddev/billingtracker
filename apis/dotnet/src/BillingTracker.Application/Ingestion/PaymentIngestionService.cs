using System.Text.Json;

namespace BillingTracker.Application.Ingestion;

public class PaymentIngestionService
{
    private readonly IPaymentIngestionRepository _repository;

    public PaymentIngestionService(IPaymentIngestionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentIngestionResult> IngestPaymentsAsync(
        PaymentIngestionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Payments == null || request.Payments.Count == 0)
        {
            throw new ArgumentException("Payments collection cannot be empty", nameof(request));
        }

        var sourceSystemId = await _repository.ResolveSourceSystemIdAsync(
            request.SourceSystem,
            cancellationToken);

        if (sourceSystemId == null)
        {
            throw new InvalidOperationException($"Source system '{request.SourceSystem}' not found");
        }

        var clientId = await _repository.ResolveClientIdAsync(
            sourceSystemId.Value,
            request.ClientExternalId,
            cancellationToken);

        if (clientId == null)
        {
            throw new InvalidOperationException(
                $"Client mapping not found for external ID '{request.ClientExternalId}' and source system '{request.SourceSystem}'");
        }

        var ingestionRunId = await _repository.CreateIngestionRunAsync(
            clientId.Value,
            sourceSystemId.Value,
            "payment_import",
            request.Payments.Count,
            cancellationToken);

        var itemResults = new List<PaymentIngestionItemResult>();
        var recordsInserted = 0;
        var recordsUpdated = 0;
        var recordsFailed = 0;

        foreach (var paymentItem in request.Payments)
        {
            var itemResult = await ProcessPaymentItemAsync(
                paymentItem,
                clientId.Value,
                sourceSystemId.Value,
                ingestionRunId,
                cancellationToken);

            itemResults.Add(itemResult);

            switch (itemResult.Status)
            {
                case PaymentIngestionItemStatus.Inserted:
                    recordsInserted++;
                    break;
                case PaymentIngestionItemStatus.Updated:
                    recordsUpdated++;
                    break;
                case PaymentIngestionItemStatus.SkippedStale:
                    // Skipped records are not counted as failures
                    break;
                case PaymentIngestionItemStatus.Failed:
                    recordsFailed++;
                    break;
            }
        }

        var status = DetermineOverallStatus(request.Payments.Count, recordsFailed);

        await _repository.CompleteIngestionRunAsync(
            ingestionRunId,
            status,
            recordsInserted,
            recordsUpdated,
            recordsFailed,
            cancellationToken);

        return new PaymentIngestionResult
        {
            IngestionRunId = ingestionRunId,
            Status = status,
            RecordsReceived = request.Payments.Count,
            RecordsInserted = recordsInserted,
            RecordsUpdated = recordsUpdated,
            RecordsFailed = recordsFailed,
            ItemResults = itemResults
        };
    }

    private async Task<PaymentIngestionItemResult> ProcessPaymentItemAsync(
        PaymentIngestionItem item,
        Guid clientId,
        Guid sourceSystemId,
        Guid ingestionRunId,
        CancellationToken cancellationToken)
    {
        try
        {
            var guarantorId = await _repository.ResolveGuarantorIdAsync(
                clientId,
                sourceSystemId,
                item.ExternalGuarantorId,
                cancellationToken);

            if (guarantorId == null)
            {
                return await RecordFailureAsync(
                    item.ExternalPaymentId,
                    ingestionRunId,
                    "GUARANTOR_NOT_FOUND",
                    $"Guarantor '{item.ExternalGuarantorId}' not found",
                    item,
                    cancellationToken);
            }

            var dependentId = await _repository.ResolveDependentIdAsync(
                clientId,
                sourceSystemId,
                item.ExternalDependentId,
                cancellationToken);

            if (dependentId == null)
            {
                return await RecordFailureAsync(
                    item.ExternalPaymentId,
                    ingestionRunId,
                    "DEPENDENT_NOT_FOUND",
                    $"Dependent '{item.ExternalDependentId}' not found",
                    item,
                    cancellationToken);
            }

            var locationId = await _repository.ResolveLocationIdAsync(
                clientId,
                sourceSystemId,
                item.ExternalLocationId,
                cancellationToken);

            if (locationId == null)
            {
                return await RecordFailureAsync(
                    item.ExternalPaymentId,
                    ingestionRunId,
                    "LOCATION_NOT_FOUND",
                    $"Location '{item.ExternalLocationId}' not found",
                    item,
                    cancellationToken);
            }

            var command = new PaymentCommand
            {
                ClientId = clientId,
                SourceSystemId = sourceSystemId,
                LocationId = locationId.Value,
                GuarantorId = guarantorId.Value,
                DependentId = dependentId.Value,
                ExternalPaymentId = item.ExternalPaymentId,
                Amount = item.Amount,
                PaymentDate = item.PaymentDate,
                PaymentStatus = item.PaymentStatus,
                SourceUpdatedAt = item.SourceUpdatedAt
            };

            var upsertResult = await _repository.UpsertPaymentAsync(command, cancellationToken);

            return new PaymentIngestionItemResult
            {
                ExternalPaymentId = item.ExternalPaymentId,
                Status = upsertResult.Status,
                ErrorMessage = upsertResult.ErrorMessage,
                PaymentId = upsertResult.PaymentId
            };
        }
        catch (Exception ex)
        {
            return await RecordFailureAsync(
                item.ExternalPaymentId,
                ingestionRunId,
                "UNEXPECTED_ERROR",
                ex.Message,
                item,
                cancellationToken);
        }
    }

    private async Task<PaymentIngestionItemResult> RecordFailureAsync(
        string externalPaymentId,
        Guid ingestionRunId,
        string errorCode,
        string errorMessage,
        PaymentIngestionItem item,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(item);

        await _repository.InsertIngestionErrorAsync(
            ingestionRunId,
            "payment",
            externalPaymentId,
            errorCode,
            errorMessage,
            payload,
            cancellationToken);

        return new PaymentIngestionItemResult
        {
            ExternalPaymentId = externalPaymentId,
            Status = PaymentIngestionItemStatus.Failed,
            ErrorMessage = errorMessage
        };
    }

    private static PaymentIngestionRunStatus DetermineOverallStatus(int recordsReceived, int recordsFailed)
    {
        if (recordsFailed == recordsReceived)
        {
            return PaymentIngestionRunStatus.Failed;
        }

        if (recordsFailed > 0)
        {
            return PaymentIngestionRunStatus.Partial;
        }

        return PaymentIngestionRunStatus.Completed;
    }
}
