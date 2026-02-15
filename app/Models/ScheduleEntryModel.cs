using System.ComponentModel.DataAnnotations;
using App.Enums;
using NodaTime;

namespace App.Models;

public class ScheduleEntryModel
{
    public int UserId { get; set; }
    public int IndexedAnimeId { get; set; }
    public UserModel? User { get; set; }
    public IndexedAnimeModel? IndexedAnime { get; set; }

    [EnumDataType(typeof(EWeekday))]
    public required EWeekday? DayOfWeek { get; set; }
    public required LocalTime? LocalTime { get; set; }
}