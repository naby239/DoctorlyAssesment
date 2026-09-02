using Doctorly.Scheduling.Application.Notifications;
using Doctorly.Scheduling.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace Doctorly.Scheduling.Infrastructure.Tests;

public sealed class NotificationDispatcherTests
{
    private sealed class RecordingChannel(string name, bool enabled = true, bool throws = false)
        : INotificationChannel
    {
        public List<string> Sent { get; } = [];

        public string Name => name;

        public bool IsEnabled => enabled;

        public Task SendAsync(
            EventNotification notification,
            NotificationRecipient recipient,
            CancellationToken cancellationToken)
        {
            if (throws)
            {
                throw new InvalidOperationException("The mail server is unreachable.");
            }

            Sent.Add(recipient.Email);

            return Task.CompletedTask;
        }
    }

    private static EventNotification ANotification(params string[] recipients) =>
        new(
            NotificationKind.EventScheduled,
            Guid.NewGuid(),
            "Consultation",
            null,
            new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 14, 9, 30, 0, DateTimeKind.Utc),
            null,
            recipients.Select(r => new NotificationRecipient(r, r)).ToList());

    private static NotificationDispatcher CreateDispatcher(params INotificationChannel[] channels) =>
        new(channels, NullLogger<NotificationDispatcher>.Instance);

    [Fact]
    public async Task Every_recipient_is_notified_on_every_enabled_channel()
    {
        var email = new RecordingChannel("Email");
        var push = new RecordingChannel("Push");

        await CreateDispatcher(email, push).DispatchAsync(
            ANotification("anna@practice.de", "klein@practice.de"),
            CancellationToken.None);

        email.Sent.ShouldBe(["anna@practice.de", "klein@practice.de"]);
        push.Sent.ShouldBe(["anna@practice.de", "klein@practice.de"]);
    }

    [Fact]
    public async Task A_disabled_channel_sends_nothing()
    {
        var disabled = new RecordingChannel("WhatsApp", enabled: false);

        await CreateDispatcher(disabled).DispatchAsync(
            ANotification("anna@practice.de"),
            CancellationToken.None);

        disabled.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_channel_that_fails_does_not_stop_the_others()
    {
        var broken = new RecordingChannel("Email", throws: true);
        var working = new RecordingChannel("Push");

        // The appointment is already committed, so a channel failing must not surface as an error.
        await CreateDispatcher(broken, working).DispatchAsync(
            ANotification("anna@practice.de"),
            CancellationToken.None);

        working.Sent.ShouldBe(["anna@practice.de"]);
    }

    [Fact]
    public async Task A_notification_with_no_recipients_is_a_no_op()
    {
        var email = new RecordingChannel("Email");

        await CreateDispatcher(email).DispatchAsync(ANotification(), CancellationToken.None);

        email.Sent.ShouldBeEmpty();
    }
}
