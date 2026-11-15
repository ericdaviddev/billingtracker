using BillingTracker.Application.Ingestion;
using Moq;

namespace BillingTracker.Application.Tests.Services;

public class PaymentIngestionServiceTests
{
    private readonly Mock<IPaymentIngestionRepository> _mockRepository;
    private readonly PaymentIngestionService _service;

    public PaymentIngestionServiceTests()
    {
        _mockRepository = new Mock<IPaymentIngestionRepository>();
        _service = new PaymentIngestionService(_mockRepository.Object);
    }

    [Fact]
    public async Task IngestPaymentsAsync_WithEmptyPayments_ThrowsArgumentException()
    {
        var request = new PaymentIngestionRequest
        {
            ClientExternalId = "test-client",
            SourceSystem = "TestSystem",
            Payments = Array.Empty<PaymentIngestionItem>()
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.IngestPaymentsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task IngestPaymentsAsync_WithInvalidSourceSystem_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new PaymentIngestionRequest
        {
            ClientExternalId = "test-client",
            SourceSystem = "InvalidSystem",
            Payments = new[]
            {
                new PaymentIngestionItem
                {
                    ExternalPaymentId = "PAY-001",
                    ExternalGuarantorId = "G-001",
                    ExternalDependentId = "D-001",
                    ExternalLocationId = "L-001",
                    Amount = 100.00m,
                    PaymentDate = DateTimeOffset.UtcNow,
                    PaymentStatus = "posted",
                    SourceUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        _mockRepository
            .Setup(r => r.ResolveSourceSystemIdAsync("InvalidSystem", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.IngestPaymentsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task IngestPaymentsAsync_WithSingleNewPayment_CompletesRunWithOneInsert()
    {
        // Arrange
        var sourceSystemId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var ingestionRunId = Guid.NewGuid();
        var guarantorId = Guid.NewGuid();
        var dependentId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var request = new PaymentIngestionRequest()
        {
            ClientExternalId = "test-client",
            SourceSystem = "TestSystem",
            Payments = new[]
            {
                new PaymentIngestionItem()
                {
                    ExternalPaymentId = "PAY-001",
                    ExternalGuarantorId = "G-001",
                    ExternalDependentId = "D-001",
                    ExternalLocationId = "L-001",
                    Amount = 100.00m,
                    PaymentDate = DateTimeOffset.UtcNow,
                    PaymentStatus = "posted",
                    SourceUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        _mockRepository
            .Setup(r => r.ResolveClientIdAsync(
                sourceSystemId,
                "test-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientId);
        _mockRepository
            .Setup(r => r.ResolveSourceSystemIdAsync("TestSystem", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceSystemId);
        _mockRepository
            .Setup(r => r.ResolveGuarantorIdAsync(clientId, sourceSystemId, "G-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(guarantorId);

        _mockRepository
            .Setup(r => r.ResolveDependentIdAsync(clientId, sourceSystemId, "D-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependentId);

        _mockRepository
            .Setup(r => r.ResolveLocationIdAsync(clientId, sourceSystemId, "L-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(locationId);

        _mockRepository
            .Setup(r => r.CreateIngestionRunAsync(clientId, sourceSystemId, "payment_import", 1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingestionRunId);


        var command = new PaymentCommand
        {
            ClientId = clientId,
            SourceSystemId = sourceSystemId,
            LocationId = locationId,
            GuarantorId = guarantorId,
            DependentId = dependentId,
            ExternalPaymentId = "PAY-001",
            Amount = 100,
            PaymentDate = request.Payments[0].PaymentDate,
            PaymentStatus = request.Payments[0].PaymentStatus,
            SourceUpdatedAt = request.Payments[0].SourceUpdatedAt
        };

        _mockRepository.Setup(r => r.UpsertPaymentAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentUpsertResult
                { PaymentId = paymentId, Status = PaymentIngestionItemStatus.Inserted });

        var result = await _service.IngestPaymentsAsync(request, CancellationToken.None);
        Assert.Equal(ingestionRunId, result.IngestionRunId);
        Assert.Equal(PaymentIngestionRunStatus.Completed, result.Status);
        Assert.Equal(1, result.RecordsReceived);
        Assert.Equal(1, result.RecordsInserted);
        Assert.Equal(0, result.RecordsUpdated);
        Assert.Equal(0, result.RecordsFailed);

        var itemResult = Assert.Single(result.ItemResults);
        Assert.Equal("PAY-001", itemResult.ExternalPaymentId);
        Assert.Equal(PaymentIngestionItemStatus.Inserted, itemResult.Status);
        Assert.Equal(paymentId, itemResult.PaymentId);
        Assert.Null(itemResult.ErrorMessage);

        _mockRepository.Verify(r => r.CompleteIngestionRunAsync(
            ingestionRunId,
            PaymentIngestionRunStatus.Completed,
            1,
            0,
            0,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task IngestPaymentsAsync_WhenPaymentUnchanged_CompletesRunWithNoCounts()
    {
        // Arrange
        var sourceSystemId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var ingestionRunId = Guid.NewGuid();

        var request = new PaymentIngestionRequest
        {
            ClientExternalId = "test-client",
            SourceSystem = "TestSystem",
            Payments = new[]
            {
                new PaymentIngestionItem
                {
                    ExternalPaymentId = "PAY-001",
                    ExternalGuarantorId = "G-001",
                    ExternalDependentId = "D-001",
                    ExternalLocationId = "L-001",
                    Amount = 100.00m,
                    PaymentDate = DateTimeOffset.UtcNow,
                    PaymentStatus = "posted",
                    SourceUpdatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        _mockRepository
            .Setup(r => r.ResolveSourceSystemIdAsync("TestSystem", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceSystemId);
        _mockRepository
            .Setup(r => r.ResolveClientIdAsync(sourceSystemId, "test-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientId);
        _mockRepository
            .Setup(r => r.ResolveGuarantorIdAsync(clientId, sourceSystemId, "G-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockRepository
            .Setup(r => r.ResolveDependentIdAsync(clientId, sourceSystemId, "D-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockRepository
            .Setup(r => r.ResolveLocationIdAsync(clientId, sourceSystemId, "L-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockRepository
            .Setup(r => r.CreateIngestionRunAsync(clientId, sourceSystemId, "payment_import", 1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingestionRunId);
        _mockRepository
            .Setup(r => r.UpsertPaymentAsync(It.IsAny<PaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentUpsertResult { Status = PaymentIngestionItemStatus.SkippedUnchanged });

        // Act
        var result = await _service.IngestPaymentsAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(PaymentIngestionRunStatus.Completed, result.Status);
        Assert.Equal(1, result.RecordsReceived);
        Assert.Equal(0, result.RecordsInserted);
        Assert.Equal(0, result.RecordsUpdated);
        Assert.Equal(0, result.RecordsFailed);

        var itemResult = Assert.Single(result.ItemResults);
        Assert.Equal(PaymentIngestionItemStatus.SkippedUnchanged, itemResult.Status);
        Assert.Null(itemResult.PaymentId);

        _mockRepository.Verify(r => r.CompleteIngestionRunAsync(
            ingestionRunId,
            PaymentIngestionRunStatus.Completed,
            0,
            0,
            0,
            CancellationToken.None), Times.Once);
    }
}