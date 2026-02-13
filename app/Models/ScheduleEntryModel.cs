using System.ComponentModel.DataAnnotations;
using App.Enums;

namespace App.Models;

public class ScheduleEntryModel
{
    public required UserModel User { get; set; }
    public required IndexedAnimeModel IndexedAnime { get; set; }

    [EnumDataType(typeof(EWeekday))]
    public EWeekday? WatchDay { get; set; }
    public TimeOnly? Time { get; set; }
}