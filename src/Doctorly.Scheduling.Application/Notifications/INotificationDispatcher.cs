namespace Doctorly.Scheduling.Application.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAsync(EventNotification notification, CancellationToken cancellationToken);
}
