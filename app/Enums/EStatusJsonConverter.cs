using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Enums;

// En anpassad JSON-konverterare för EStatus-enum som hanterar specifika strängrepresentationer av enum-värdena.
public class EStatusJsonConverter : JsonConverter<EStatus>
{
    // Konverterar en JSON-sträng till en EStatus-enum.
    public override EStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "Not yet aired" => EStatus.NotYetAired,
            "Currently Airing" => EStatus.CurrentlyAiring,
            "Finished Airing" => EStatus.FinishedAiring,
            _ => throw new JsonException($"Unable to convert \"{value}\" to EStatus.")
        };
    }

    // Konverterar en EStatus-enum till en JSON-sträng.
    public override void Write(Utf8JsonWriter writer, EStatus value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            EStatus.NotYetAired => "Not yet aired",
            EStatus.CurrentlyAiring => "Currently Airing",
            EStatus.FinishedAiring => "Finished Airing",
            _ => throw new JsonException($"Unable to convert EStatus value \"{value}\" to string.")
        };
        writer.WriteStringValue(str);
    }
}