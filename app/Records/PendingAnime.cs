using App.Enums;
using NodaTime;

namespace App.Records;

public record PendingAnime
{
    public required int Mal_ID { get; set; }
    public required string Title { get; set; }
    public required string ImageURL { get; set; }
    public required EStatus Status { get; set; }
    public int? TotalEpisodes { get; set; }
    public required Instant ReleaseInstant { get; set; }
}