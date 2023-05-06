using System.Text.Json.Serialization;
using System.Text.Json;

namespace BuildingBlocks.JsonConverters;
public sealed class TimeSpanConverter : JsonConverter<TimeSpan>
{
#pragma warning disable CS8604 // Possible null reference argument.
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.Parse(reader.GetString());
#pragma warning restore CS8604 // Possible null reference argument.

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
