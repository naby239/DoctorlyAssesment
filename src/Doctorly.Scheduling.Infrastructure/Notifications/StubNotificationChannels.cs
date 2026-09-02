using Doctorly.Scheduling.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doctorly.Scheduling.Infrastructure.Notifications;

// WhatsApp and push both need provider onboarding and credentials that are out of scope here.
// They are registered as real channels so the dispatch path is identical, and log what they
// would have sent - swapping in a provider client is a change confined to these classes.
internal sealed class WhatsAppNotificationChannel(
    IOptions<NotificationOptions> options,
    ILogger<WhatsAppNotificationChannel> logger)
    : INotificationChannel
{
    public string Name => "WhatsApp";

    public bool IsEnabled => options.Value.WhatsAppEnabled;

    public Task SendAsync(
        EventNotification notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "WhatsApp channel is not wired to a provider. Would send {Kind} for event {EventId} to {Recipient}.",
            notification.Kind,
            notification.EventId,
            recipient.Name);

        return Task.CompletedTask;
    }
}

internal sealed class PushNotificationChannel(
    IOptions<NotificationOptions> options,
    ILogger<PushNotificationChannel> logger)
    : INotificationChannel
{
    public string Name => "Push";

    public bool IsEnabled => options.Value.PushEnabled;

    public Task SendAsync(
        EventNotification notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Push channel is not wired to a provider. Would send {Kind} for event {EventId} to {Recipient}.",
            notification.Kind,
            notification.EventId,
            recipient.Name);

        return Task.CompletedTask;
    }
}
