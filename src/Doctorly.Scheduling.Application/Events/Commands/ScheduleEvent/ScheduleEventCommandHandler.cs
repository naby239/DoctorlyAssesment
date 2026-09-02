using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Domain.Scheduling;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;

public sealed class ScheduleEventCommandHandler(
    ICalendarEventRepository events,
    IAttendeeRepository attendees,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ScheduleEventCommand, EventDto>
{
    public async Task<EventDto> Handle(ScheduleEventCommand request, CancellationToken cancellationToken)
    {
        var calendarEvent = CalendarEvent.Schedule(
            request.Title,
            request.Description,
            request.StartTime,
            request.EndTime);

        var invited = new List<(Attendee Attendee, bool OptInNotify)>();

        foreach (var requested in request.Attendees ?? [])
        {
            var attendee = await ResolveAttendeeAsync(requested, cancellationToken).ConfigureAwait(false);

            calendarEvent.Invite(attendee.Id, requested.OptInNotify);
            invited.Add((attendee, requested.OptInNotify));
        }

        await events.AddAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Map(calendarEvent, invited);
    }

    // Attendees are reused across appointments, so an existing record wins over a new one.
    private async Task<Attendee> ResolveAttendeeAsync(
        ScheduleEventAttendee requested,
        CancellationToken cancellationToken)
    {
        var email = Attendee.NormaliseEmail(requested.Email ?? string.Empty);

        var existing = await attendees.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var attendee = Attendee.Register(requested.Name, requested.Email, requested.ContactNumber);
        await attendees.AddAsync(attendee, cancellationToken).ConfigureAwait(false);

        return attendee;
    }

    private static EventDto Map(
        CalendarEvent calendarEvent,
        IReadOnlyList<(Attendee Attendee, bool OptInNotify)> invited)
    {
        var byId = invited.ToDictionary(i => i.Attendee.Id, i => i.Attendee);

        var attendeeDtos = calendarEvent.Attendees
            .Select(a => new EventAttendeeDto(
                a.AttendeeId,
                byId[a.AttendeeId].Name,
                byId[a.AttendeeId].Email,
                a.Status.ToString(),
                a.OptInNotify))
            .ToList();

        return new EventDto(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.Status.ToString(),
            calendarEvent.CancellationReason,
            calendarEvent.CancelledAt,
            calendarEvent.Version,
            attendeeDtos);
    }
}
