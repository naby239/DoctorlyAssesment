namespace Doctorly.Scheduling.Application.Notifications;

public enum NotificationKind
{
    EventScheduled,
    EventUpdated,
    EventCancelled,
}

public sealed record NotificationRecipient(string Name, string Email);

public sealed record EventNotification(
    NotificationKind Kind,
    Guid EventId,
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    string? CancellationReason,
    IReadOnlyList<NotificationRecipient> Recipients);
