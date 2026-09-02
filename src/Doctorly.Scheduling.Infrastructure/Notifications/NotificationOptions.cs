namespace Doctorly.Scheduling.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string FromName { get; set; } = "Doctorly Practice";

    public string FromAddress { get; set; } = "appointments@doctorly.de";

    public EmailOptions Email { get; set; } = new();

    public bool WhatsAppEnabled { get; set; }

    public bool PushEnabled { get; set; }
}

public enum EmailDelivery
{
    // Writes the message to disk. The default, so the API needs no mail server to run.
    FileDrop,

    // Real SMTP. Point it at smtp4dev (dotnet tool install -g Rnwood.Smtp4dev) to watch
    // messages arrive without configuring a mail account.
    Smtp,
}

public sealed class EmailOptions
{
    public bool Enabled { get; set; } = true;

    public EmailDelivery Delivery { get; set; } = EmailDelivery.FileDrop;

    public string DropDirectory { get; set; } = "artifacts/notifications";

    public string SmtpHost { get; set; } = "localhost";

    public int SmtpPort { get; set; } = 2525;
}
