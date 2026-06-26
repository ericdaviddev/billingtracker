using BillingTracker.Application.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace BillingTracker.Api.Controllers;

[ApiController]
[Route("api/ingestion")]
public class PaymentIngestionController : ControllerBase
{
    private readonly PaymentIngestionService _ingestionService;
    private readonly ILogger<PaymentIngestionController> _logger;

    public PaymentIngestionController(
        PaymentIngestionService ingestionService,
        ILogger<PaymentIngestionController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost("payments")]
    [ProducesResponseType(typeof(PaymentIngestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentIngestionResult>> IngestPayments(
        [FromBody] PaymentIngestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Starting payment ingestion for client {ClientExternalId} from source system {SourceSystem} with {PaymentCount} payments",
                request.ClientExternalId,
                request.SourceSystem,
                request.Payments?.Count ?? 0);

            var result = await _ingestionService.IngestPaymentsAsync(request, cancellationToken);

            _logger.LogInformation(
                "Completed payment ingestion run {IngestionRunId} with status {Status}. Inserted: {Inserted}, Updated: {Updated}, Failed: {Failed}",
                result.IngestionRunId,
                result.Status,
                result.RecordsInserted,
                result.RecordsUpdated,
                result.RecordsFailed);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid payment ingestion request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment ingestion failed due to invalid operation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment ingestion");
            return StatusCode(500, new { error = "An unexpected error occurred during payment ingestion" });
        }
    }
}
