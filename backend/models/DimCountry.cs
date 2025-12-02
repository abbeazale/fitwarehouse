namespace backend.Models;

public class DimCountry
{
    public int CountryKey { get; set; }
    public required string CountryName { get; set; }
    public string? IsoCode { get; set; }
    public string? Region { get; set; }
}

