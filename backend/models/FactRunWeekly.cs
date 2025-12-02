namespace backend.Models;

public class FactRunWeekly
{
    public DateOnly DateKey { get; set; }
    public int AthleteKey { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal? DurationMin { get; set; }
    public decimal? PaceMinPerKm { get; set; }
    public decimal? Load7dKm { get; set; }
    public decimal? Load28dKm { get; set; }
    public decimal? AcuteChronicRatio { get; set; }
    public bool ZeroDistanceFlag { get; set; }
}

