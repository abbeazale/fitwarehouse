namespace backend.Models;

public class StagingIngestLog
{
    public long Id { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public DateTime LoadTimestampUtc { get; set; }
    public int? RowCount { get; set; }
    public string? Checksum { get; set; }
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }
}

