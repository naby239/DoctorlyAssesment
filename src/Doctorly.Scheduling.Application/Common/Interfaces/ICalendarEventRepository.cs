using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Application.Common.Interfaces;

public interface ICalendarEventRepository
{
    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);

    // Tracked, unlike the read side - the caller is going to modify it.
    Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
