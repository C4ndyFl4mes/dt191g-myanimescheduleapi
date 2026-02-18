using NodaTime;

namespace App.DTOs;

public record PostGetRequest
{
    public required int TargetID { get; set; }
    public required int Page { get; set; }
    public required string TimeZone { get; set; }
    public readonly int PerPage = 5;
}