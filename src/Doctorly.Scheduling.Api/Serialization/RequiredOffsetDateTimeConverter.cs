using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Doctorly.Scheduling.Api.Serialization;

// Without an offset, .NET reads a timestamp in the server's local time, so the same request
// would store a different instant depending on where the API runs. An appointment time is
// worth rejecting rather than guessing at.
public sealed class RequiredOffsetDateTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var text = reader.GetString();

        if (string.IsNullOrWhiteSpace(text) || !TimestampOffset.HasOffset(text))
        {
            throw new JsonException(TimestampOffset.ErrorMessage);
        }

        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
