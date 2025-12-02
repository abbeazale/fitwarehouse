namespace backend.Models;

public class DimAgeGroup
{
    public int AgeGroupKey { get; set; }
    public required string AgeGroupLabel { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}

