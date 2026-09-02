using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Infrastructure.Persistence.Repositories;

internal sealed class CalendarEventRepository(SchedulingDbContext context) : ICalendarEventRepository
{
    public async Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken) =>
        await context.Events.AddAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
}
