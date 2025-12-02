namespace backend.Models;

public class DimGender
{
    public int GenderKey { get; set; }
    public required string GenderCode { get; set; }
    public string? GenderLabel { get; set; }
}

