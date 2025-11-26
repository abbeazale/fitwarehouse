namespace backend.Models;

public record InventoryRecordDto(
    Guid Id,
    string ProductName,
    int Quantity,
    string WarehouseLocation,
    string SubmittedBy,
    DateTime ProcessedAtUtc);

