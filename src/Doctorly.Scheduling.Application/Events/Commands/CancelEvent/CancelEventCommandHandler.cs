using Doctorly.Scheduling.Application.Common.Exceptions;
using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Application.Notifications;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.CancelEvent;

public sealed class CancelEventCommandHandler(
    ICalendarEventRepository events,
    IUnitOfWork unitOfWork,
    IEventQueries queries,
    INotificationDispatcher notifications)
    : IRequestHandler<CancelEventCommand, EventDto?>
{
    public async Task<EventDto?> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var calendarEvent = await events.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (calendarEvent is null)
        {
            return null;
        }

        if (calendarEvent.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "This event has changed since you last read it. Fetch it again and reapply your changes.");
        }

        // A state transition, not a delete. The row and its invitations stay.
        calendarEvent.Cancel(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var updated = await queries.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (updated is not null)
        {
            await notifications
                .DispatchAsync(
                    EventNotificationFactory.From(updated, NotificationKind.EventCancelled),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return updated;
    }
}
