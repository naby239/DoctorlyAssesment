using System.Globalization;
using Doctorly.Scheduling.Application.Notifications;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Doctorly.Scheduling.Infrastructure.Notifications;

internal sealed class EmailNotificationChannel(
    IOptions<NotificationOptions> options,
    ILogger<EmailNotificationChannel> logger)
    : INotificationChannel
{
    private readonly NotificationOptions _options = options.Value;

    public string Name => "Email";

    public bool IsEnabled => _options.Email.Enabled;

    public async Task SendAsync(
        EventNotification notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken)
    {
        var message = BuildMessage(notification, recipient);

        switch (_options.Email.Delivery)
        {
            case EmailDelivery.Smtp:
                await SendOverSmtpAsync(message, cancellationToken).ConfigureAwait(false);
                break;

            case EmailDelivery.FileDrop:
            default:
                await WriteToDiskAsync(message, notification, recipient, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }
    }

    private MimeMessage BuildMessage(EventNotification notification, NotificationRecipient recipient)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(recipient.Name, recipient.Email));
        message.Subject = Subject(notification);
        message.Body = new TextPart("plain") { Text = Body(notification, recipient) };

        return message;
    }

    private static string Subject(EventNotification notification) => notification.Kind switch
    {
        NotificationKind.EventCancelled => $"Cancelled: {notification.Title}",
        NotificationKind.EventUpdated => $"Updated: {notification.Title}",
        _ => $"Invitation: {notification.Title}",
    };

    private static string Body(EventNotification notification, NotificationRecipient recipient)
    {
        var when = notification.StartTime.ToString("dddd d MMMM yyyy, HH:mm", CultureInfo.InvariantCulture);

        return notification.Kind switch
        {
            NotificationKind.EventCancelled =>
                $"Hello {recipient.Name},\n\nYour appointment \"{notification.Title}\" on {when} UTC has been cancelled."
                + (string.IsNullOrWhiteSpace(notification.CancellationReason)
                    ? "\n"
                    : $"\n\nReason: {notification.CancellationReason}\n"),

            NotificationKind.EventUpdated =>
                $"Hello {recipient.Name},\n\nYour appointment \"{notification.Title}\" has changed."
                + $"\n\nIt now starts on {when} UTC.\n",

            _ =>
                $"Hello {recipient.Name},\n\nYou have been invited to \"{notification.Title}\""
                + $" on {when} UTC.\n\nPlease let the practice know whether you can attend.\n",
        };
    }

    private async Task SendOverSmtpAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(
            _options.Email.SmtpHost,
            _options.Email.SmtpPort,
            MailKit.Security.SecureSocketOptions.Auto,
            cancellationToken).ConfigureAwait(false);

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Sent {Subject} over SMTP to {Host}:{Port}",
            message.Subject,
            _options.Email.SmtpHost,
            _options.Email.SmtpPort);
    }

    private async Task WriteToDiskAsync(
        MimeMessage message,
        EventNotification notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(_options.Email.DropDirectory);
        Directory.CreateDirectory(directory);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var safeRecipient = recipient.Email.Replace('@', '_').Replace('/', '_');
        var path = Path.Combine(directory, $"{stamp}-{notification.Kind}-{safeRecipient}.eml");

        await using (var stream = File.Create(path))
        {
            await message.WriteToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Wrote {Subject} to {Path}", message.Subject, path);
    }
}
