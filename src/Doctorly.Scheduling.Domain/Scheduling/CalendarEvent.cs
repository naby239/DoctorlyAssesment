using Doctorly.Scheduling.Domain.Common;

namespace Doctorly.Scheduling.Domain.Scheduling;

public sealed class CalendarEvent
{
    private readonly List<EventAttendee> _attendees = [];

    private CalendarEvent()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateTime StartTime { get; private set; }

    public DateTime EndTime { get; private set; }

    public EventStatus Status { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Concurrency token, surfaced over HTTP as an ETag. Changes on every write.
    public Guid Version { get; private set; }

    public IReadOnlyCollection<EventAttendee> Attendees => _attendees.AsReadOnly();

    public bool IsCancelled => Status == EventStatus.Cancelled;

    public static CalendarEvent Schedule(
        string? title,
        string? description,
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        var (start, end) = ValidatePeriod(startTime, endTime);

        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = ValidateTitle(title),
            Description = ValidateDescription(description),
            StartTime = start,
            EndTime = end,
            Status = EventStatus.Scheduled,
            CreatedAt = DateTime.UtcNow,
            Version = Guid.NewGuid(),
        };
    }

    public void UpdateDetails(string? title, string? description)
    {
        EnsureModifiable();

        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        MarkUpdated();
    }

    public void Reschedule(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        EnsureModifiable();

        var (start, end) = ValidatePeriod(startTime, endTime);

        StartTime = start;
        EndTime = end;
        MarkUpdated();
    }

    public void Cancel(string? reason = null)
    {
        if (IsCancelled)
        {
            throw new DomainException("This event has already been cancelled.");
        }

        if (reason is { Length: > SchedulingLimits.CancellationReasonMaxLength })
        {
            throw new DomainException(
                $"A cancellation reason cannot exceed {SchedulingLimits.CancellationReasonMaxLength} characters.");
        }

        Status = EventStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CancelledAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public EventAttendee Invite(Guid attendeeId, bool optInNotify = true)
    {
        EnsureModifiable();

        if (_attendees.Any(a => a.AttendeeId == attendeeId))
        {
            throw new DomainException("That attendee has already been invited to this event.");
        }

        var invitation = EventAttendee.Create(Id, attendeeId, optInNotify);
        _attendees.Add(invitation);
        MarkUpdated();

        return invitation;
    }

    public void RespondToInvitation(Guid attendeeId, AttendanceStatus response)
    {
        if (IsCancelled)
        {
            throw new DomainException("This event has been cancelled and no longer accepts responses.");
        }

        var invitation = _attendees.SingleOrDefault(a => a.AttendeeId == attendeeId)
            ?? throw new DomainException("That attendee was not invited to this event.");

        invitation.Respond(response);
        MarkUpdated();
    }

    private void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
    }

    private void EnsureModifiable()
    {
        if (IsCancelled)
        {
            throw new DomainException("A cancelled event cannot be modified.");
        }
    }

    // Callers may send any offset; only UTC is stored. SQLite cannot sort DateTimeOffset,
    // and since everything is normalised the offset carried no information anyway.
    private static (DateTime Start, DateTime End) ValidatePeriod(
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        var start = startTime.UtcDateTime;
        var end = endTime.UtcDateTime;

        if (end <= start)
        {
            throw new DomainException("An event must end after it starts.");
        }

        return (start, end);
    }

    private static string ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An event must have a title.");
        }

        var trimmed = title.Trim();

        if (trimmed.Length > SchedulingLimits.EventTitleMaxLength)
        {
            throw new DomainException(
                $"An event title cannot exceed {SchedulingLimits.EventTitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();

        if (trimmed.Length > SchedulingLimits.EventDescriptionMaxLength)
        {
            throw new DomainException(
                $"An event description cannot exceed {SchedulingLimits.EventDescriptionMaxLength} characters.");
        }

        return trimmed;
    }
}
