namespace backend.Models;

public class DimAthlete
{
    public int AthleteKey { get; set; }
    public int AthleteIdSource { get; set; }
    public int? GenderKey { get; set; }
    public int? AgeGroupKey { get; set; }
    public int? CountryKey { get; set; }
    public DateOnly? FirstSeenWeek { get; set; }
    public DateOnly? LastSeenWeek { get; set; }
}

