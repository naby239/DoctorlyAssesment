using Doctorly.Scheduling.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace Doctorly.Scheduling.Infrastructure.Notifications;

internal sealed class NotificationDispatcher(
    IEnumerable<INotificationChannel> channels,
    ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    public async Task DispatchAsync(EventNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var enabled = channels.Where(c => c.IsEnabled).ToList();

        if (enabled.Count == 0 || notification.Recipients.Count == 0)
        {
            return;
        }

        foreach (var channel in enabled)
        {
            foreach (var recipient in notification.Recipients)
            {
                try
                {
                    await channel.SendAsync(notification, recipient, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The appointment is already saved. A mail server being down must not fail
                    // the booking, so this is logged and the remaining recipients still go out.
                    logger.LogError(
                        ex,
                        "The {Channel} channel failed to notify {Recipient} about event {EventId}.",
                        channel.Name,
                        recipient.Email,
                        notification.EventId);
                }
            }
        }
    }
}
