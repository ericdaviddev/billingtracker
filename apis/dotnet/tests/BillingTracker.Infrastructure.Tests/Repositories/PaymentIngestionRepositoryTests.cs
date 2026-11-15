using BillingTracker.Application.Ingestion;
using BillingTracker.Infrastructure.Repositories;
using BillingTracker.Infrastructure.Tests.Fixtures;
using Org.BouncyCastle.Cms;

namespace BillingTracker.Infrastructure.Tests.Repositories;

public class PaymentIngestionRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PaymentIngestionRepository _repo;

    public PaymentIngestionRepositoryTests(PostgresFixture db) =>
        _repo = new PaymentIngestionRepository(db.ConnectionFactory);
    
    
    private static PaymentCommand Command(string externalId, DateTimeOffset sourceUpdatedAt, decimal amount = 100m) => new()
    {
        ClientId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        SourceSystemId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        LocationId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
        GuarantorId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
        DependentId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
        ExternalPaymentId = externalId,
        Amount = amount,
        PaymentDate = new DateTimeOffset(2026, 5, 24, 12, 30, 0, TimeSpan.Zero),
        PaymentStatus = "posted",
        SourceUpdatedAt = sourceUpdatedAt
    };
    
    [Fact]
    public async Task Same_timestamp_with_changed_amount_is_still_applied()
    {
        var ts = new DateTimeOffset(2026, 5, 24, 12, 45, 0, TimeSpan.Zero);

        await _repo.UpsertPaymentAsync(Command("TEST-SAMETS-001", ts), CancellationToken.None);
        var result = await _repo.UpsertPaymentAsync(Command("TEST-SAMETS-001", ts, amount: 150m), CancellationToken.None);

        Assert.Equal(PaymentIngestionItemStatus.Updated, result.Status);
    }
    
    [Fact]
    public async Task Replaying_identical_payload_is_skipped()
    {
        var cmd = Command("TEST-REPLAY-001", new DateTimeOffset(2026, 5, 24, 12, 45, 0, TimeSpan.Zero));

        var first = await _repo.UpsertPaymentAsync(cmd, CancellationToken.None);
        var second = await _repo.UpsertPaymentAsync(cmd, CancellationToken.None);
        
        Assert.Equal(PaymentIngestionItemStatus.Inserted, first.Status);
        Assert.Equal(PaymentIngestionItemStatus.SkippedUnchanged, second.Status);
    }

    [Fact]
    public async Task Older_source_update_is_skipped_as_stale()
    {
        var newer = Command("TEST-STALE-001", new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero));
        var older = Command("TEST-STALE-001", new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero));

        await _repo.UpsertPaymentAsync(newer, CancellationToken.None);
        var result = await _repo.UpsertPaymentAsync(older, CancellationToken.None);
        
        Assert.Equal(PaymentIngestionItemStatus.SkippedStale, result.Status);
    }
}
