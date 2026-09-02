using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandHandler(
    ICalendarEventRepository events,
    IUnitOfWork unitOfWork,
    IEventQueries queries)
    : IRequestHandler<RespondToInvitationCommand, EventDto?>
{
    public async Task<EventDto?> Handle(
        RespondToInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await events
            .GetByIdAsync(request.EventId, cancellationToken)
            .ConfigureAwait(false);

        if (calendarEvent is null)
        {
            return null;
        }

        // The invitation is addressed by a route of its own, so an attendee who was never
        // invited is a missing resource rather than a bad request.
        if (calendarEvent.Attendees.All(a => a.AttendeeId != request.AttendeeId))
        {
            return null;
        }

        calendarEvent.RespondToInvitation(request.AttendeeId, request.Response);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await queries.GetByIdAsync(request.EventId, cancellationToken).ConfigureAwait(false);
    }
}
