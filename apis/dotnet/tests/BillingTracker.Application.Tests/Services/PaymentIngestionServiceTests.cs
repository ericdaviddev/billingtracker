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

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.IngestPaymentsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task IngestPaymentsAsync_WithInvalidSourceSystem_ThrowsInvalidOperationException()
    {
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

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.IngestPaymentsAsync(request, CancellationToken.None));
    }
}
