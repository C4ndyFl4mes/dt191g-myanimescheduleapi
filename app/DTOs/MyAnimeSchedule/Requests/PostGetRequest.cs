using NodaTime;

namespace App.DTOs;

public record PostGetRequest
{
    public int? TargetID { get; set; }
    public required int Page { get; set; }
    public string? TimeZone { get; set; }
    public readonly int PerPage = 5;
}