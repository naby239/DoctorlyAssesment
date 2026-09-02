using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Application.Notifications;

internal static class EventNotificationFactory
{
    // Only attendees who opted in, and for anything other than a cancellation only those still
    // expected - there is no point telling someone who declined that the time moved.
    internal static EventNotification From(EventDto calendarEvent, NotificationKind kind)
    {
        var recipients = calendarEvent.Attendees
            .Where(a => a.OptInNotify)
            .Where(a => kind == NotificationKind.EventCancelled
                || a.Status != nameof(AttendanceStatus.Declined))
            .Select(a => new NotificationRecipient(a.Name, a.Email))
            .ToList();

        return new EventNotification(
            kind,
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.CancellationReason,
            recipients);
    }
}
