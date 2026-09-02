namespace Doctorly.Scheduling.Api.Serialization;

// Shared by the JSON converter (request bodies) and the model binder (query strings) so both
// paths apply the same rule.
internal static class TimestampOffset
{
    internal const string ErrorMessage =
        "Timestamps must state their offset, either 'Z' for UTC or a value such as '+02:00'.";

    internal static bool HasOffset(string text)
    {
        if (text.EndsWith('Z') || text.EndsWith('z'))
        {
            return true;
        }

        var timeStart = text.IndexOf('T', StringComparison.OrdinalIgnoreCase);

        if (timeStart < 0)
        {
            return false;
        }

        // Only look after the 'T' - the date itself contains hyphens.
        var time = text[timeStart..];

        return time.Contains('+', StringComparison.Ordinal) || time.LastIndexOf('-') > 0;
    }
}
