using System.Text.Json.Serialization;
using App.Enums;

namespace App.DTOs;

public record IndexableAnime
{
    public required int mal_id { get; set; }
    public int? episodes { get; set; }
    public required bool airing { get; set; }
    public required Aired aired { get; set; }
    public required Titles[] titles { get; set; }
    public required Images images { get; set; }
    public Broadcast? broadcast { get; set; }

    [JsonConverter(typeof(EStatusJsonConverter))]
    public required EStatus status { get; set; }
}

public record Broadcast
{
    public required string day { get; set; }
    public required string time { get; set; }
    public required string timezone { get; set; }
}

public record Titles
{
    public required string type { get; set; }
    public required string title { get; set; }
}

public record Images
{
    public required Webp webp { get; set; }
}

public record Webp
{
    public required string image_url { get; set; }
}