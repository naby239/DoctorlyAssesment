using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Application.Common.Interfaces;

public interface ICalendarEventRepository
{
    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);
}
