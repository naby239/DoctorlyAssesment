using Doctorly.Scheduling.Application.Notifications;

namespace Doctorly.Scheduling.Infrastructure.Notifications;

internal interface INotificationChannel
{
    string Name { get; }

    bool IsEnabled { get; }

    Task SendAsync(
        EventNotification notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken);
}
