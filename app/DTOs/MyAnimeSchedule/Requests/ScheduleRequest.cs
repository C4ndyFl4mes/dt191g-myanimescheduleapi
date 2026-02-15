using App.Enums;

namespace App.DTOs;

public record ScheduleRequest
{
    public required int Mal_ID { get; init; }
    public EWeekday? WatchDay { get; init; }
    public TimeOnly? Time { get; init; }
}