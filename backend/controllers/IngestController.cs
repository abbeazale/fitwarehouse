using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private const int MaxBatchSize = 10_000;
    private readonly IRunIngestionService _ingestionService;
    private readonly ILogger<IngestController> _logger;

    public IngestController(IRunIngestionService ingestionService, ILogger<IngestController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost("runs")]
    public async Task<ActionResult<RunIngestResponse>> IngestRuns([FromBody] List<RunIngestRow> rows, CancellationToken cancellationToken)
    {
        if (rows == null || rows.Count == 0)
        {
            return BadRequest("No rows provided.");
        }

        if (rows.Count > MaxBatchSize)
        {
            return BadRequest($"Batch too large. Max {MaxBatchSize} rows per request.");
        }

        try
        {
            var result = await _ingestionService.IngestAsync(rows, "api-run-ingest", cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest runs.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to ingest runs.");
        }
    }
}

