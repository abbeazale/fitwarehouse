using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IRunIngestionService _ingestionService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(IRunIngestionService ingestionService, ILogger<MaintenanceController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost("backfill-dimensions")]
    public async Task<IActionResult> BackfillDimensions(CancellationToken cancellationToken)
    {
        try
        {
            await _ingestionService.BackfillDimensionsFromStagingAsync(cancellationToken);
            return Ok(new { status = "ok" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill from staging failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Backfill failed. See logs for details.");
        }
    }
}
