using App.Enums;

namespace App.DTOs;

public record ScheduleUpdateRequest
{
    public required int Id { get; init; }
    public EWeekday? WatchDay { get; init; }
    public TimeOnly? Time { get; init; }
}