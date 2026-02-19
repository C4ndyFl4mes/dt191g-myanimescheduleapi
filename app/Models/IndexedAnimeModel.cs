using App.Enums;
using NodaTime;

namespace App.Models;

public sealed class IndexedAnimeModel
{
    public int Id { get; set; }
    public required int Mal_ID { get; set; }
    public required string Title { get; set; }
    public required string ImageURL { get; set; }

    public required EStatus Status { get; set; }
    public int? TotalEpisodes { get; set; }
    public required Instant ReleaseInstant { get; set; }
    public EWeekday? BroadcastWeekday { get; set; }
}