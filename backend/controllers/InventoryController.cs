using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InventoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IEnumerable<InventoryRecordDto>> GetInventory()
    {
        var items = await _context.Inventory
            .OrderByDescending(i => i.ProcessedAtUtc)
            .ToListAsync();

        return items.Select(item => new InventoryRecordDto(
            item.Id,
            item.ProductName,
            item.Quantity,
            item.WarehouseLocation,
            item.SubmittedBy,
            item.ProcessedAtUtc));
    }

    [HttpPost]
    public async Task<IActionResult> CreateInventory(CleanedInventorySubmission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.ProductName) ||
            string.IsNullOrWhiteSpace(submission.WarehouseLocation) ||
            string.IsNullOrWhiteSpace(submission.SubmittedBy) ||
            submission.Quantity <= 0)
        {
            return BadRequest("Submission is missing required fields.");
        }

        var processedAt = submission.ProcessedAtUtc == DateTime.MinValue
            ? DateTime.UtcNow
            : submission.ProcessedAtUtc.ToUniversalTime();

        var record = new InventoryRecord(
            Guid.NewGuid(),
            submission.ProductName,
            submission.Quantity,
            submission.WarehouseLocation,
            submission.SubmittedBy,
            processedAt);

        _context.Inventory.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInventory), new { id = record.Id },
            new InventoryRecordDto(record.Id, record.ProductName, record.Quantity,
                record.WarehouseLocation, record.SubmittedBy, record.ProcessedAtUtc));
    }
}

