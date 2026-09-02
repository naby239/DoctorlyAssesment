using Doctorly.Scheduling.Application.Common.Exceptions;
using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Application.Notifications;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.UpdateEvent;

public sealed class UpdateEventCommandHandler(
    ICalendarEventRepository events,
    IUnitOfWork unitOfWork,
    IEventQueries queries,
    INotificationDispatcher notifications)
    : IRequestHandler<UpdateEventCommand, EventDto?>
{
    public async Task<EventDto?> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var calendarEvent = await events.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (calendarEvent is null)
        {
            return null;
        }

        // The caller edited a version that has since moved on.
        if (calendarEvent.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "This event has changed since you last read it. Fetch it again and reapply your changes.");
        }

        calendarEvent.UpdateDetails(request.Title, request.Description);
        calendarEvent.Reschedule(request.StartTime, request.EndTime);

        // A writer that slipped in between the read above and this save is caught by the
        // concurrency token, which SaveChanges turns into a ConcurrencyConflictException.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var updated = await queries.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (updated is not null)
        {
            await notifications
                .DispatchAsync(
                    EventNotificationFactory.From(updated, NotificationKind.EventUpdated),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return updated;
    }
}
