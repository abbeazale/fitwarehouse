namespace backend.Models;

public record CleanedInventorySubmission(
    string ProductName,
    int Quantity,
    string WarehouseLocation,
    string SubmittedBy,
    DateTime ProcessedAtUtc);

