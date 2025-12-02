namespace backend.Models;

public class StagingRunRaw
{
    public long Id { get; set; }
    public DateOnly RunDate { get; set; }
    public int AthleteIdSource { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal? DurationMin { get; set; }
    public string? GenderRaw { get; set; }
    public string? AgeGroupRaw { get; set; }
    public string? CountryRaw { get; set; }
    public string? MajorsRaw { get; set; }
    public long? IngestBatchId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

