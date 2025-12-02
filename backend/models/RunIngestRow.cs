namespace backend.Models;

public class RunIngestRow
{
    public DateOnly RunDate { get; set; }
    public int AthleteIdSource { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal? DurationMin { get; set; }
    public string? Gender { get; set; }
    public string? AgeGroup { get; set; }
    public string? Country { get; set; }
    public List<string>? Majors { get; set; }
}

