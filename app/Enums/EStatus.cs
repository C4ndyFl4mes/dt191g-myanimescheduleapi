using System.Text.Json.Serialization;

namespace App.Enums;

public enum EStatus
{
    [JsonPropertyName("Not yet aired")]
    NotYetAired,
    [JsonPropertyName("Currently Airing")]
    CurrentlyAiring,
    [JsonPropertyName("Finished Airing")]
    FinishedAiring
}