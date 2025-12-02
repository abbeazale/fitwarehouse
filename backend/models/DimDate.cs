namespace backend.Models;

public class DimDate
{
    public DateOnly DateKey { get; set; }
    public int IsoYear { get; set; }
    public int IsoWeek { get; set; }
    public int Month { get; set; }
    public int Quarter { get; set; }
    public DateOnly WeekStartDate { get; set; }
}

