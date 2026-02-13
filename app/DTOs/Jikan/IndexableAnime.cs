using System.Text.Json.Serialization;
using App.Enums;

namespace App.DTOs;

public record IndexableAnime
{
    public required int mal_id { get; set; }
    public int? episodes { get; set; }
    public required bool airing { get; set; }
    public required Aired aired { get; set; }

    [JsonConverter(typeof(EStatusJsonConverter))]
    public required EStatus status { get; set; }
}