using App.Enums;

namespace App.DTOs;

public record ScheduleResponse
{
    public List<ScheduleWeekDayResponse> WeekDays { get; set; } = [];
}

public record ScheduleWeekDayResponse
{
    public required EWeekday DayOfWeek { get; set; }
    public List<ScheduleEntryResponse> ScheduleEntries { get; set; } = [];
}

public record ScheduleEntryResponse
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public required string ImageURL { get; set; }
    public required string Time { get; set; }
}