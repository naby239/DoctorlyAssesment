namespace Doctorly.Scheduling.Domain.Scheduling;

public static class SchedulingLimits
{
    public const int AttendeeNameMaxLength = 200;

    // RFC 5321 maximum.
    public const int EmailAddressMaxLength = 320;

    public const int ContactNumberMaxLength = 30;

    public const int EventTitleMaxLength = 200;
    public const int EventDescriptionMaxLength = 2000;
    public const int CancellationReasonMaxLength = 500;
}
