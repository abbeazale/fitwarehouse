namespace backend.Models;

public record InventoryRecord(
    Guid Id,
    string ProductName,
    int Quantity,
    string WarehouseLocation,
    string SubmittedBy,
    DateTime ProcessedAtUtc);

