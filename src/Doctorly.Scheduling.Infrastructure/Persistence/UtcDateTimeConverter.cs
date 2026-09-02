using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Doctorly.Scheduling.Infrastructure.Persistence;

// SQLite hands back DateTimeKind.Unspecified, which then serialises without a 'Z' and reads as
// local time to a client. Everything stored is UTC, so say so on the way out.
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
