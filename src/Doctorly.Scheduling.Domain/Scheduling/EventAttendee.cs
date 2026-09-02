using Doctorly.Scheduling.Domain.Common;

namespace Doctorly.Scheduling.Domain.Scheduling;

public sealed class EventAttendee
{
    private EventAttendee()
    {
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid AttendeeId { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public bool OptInNotify { get; private set; }

    public DateTimeOffset InvitedAt { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    // internal so an invitation can only be created through CalendarEvent.Invite.
    internal static EventAttendee Create(Guid eventId, Guid attendeeId, bool optInNotify)
    {
        if (attendeeId == Guid.Empty)
        {
            throw new DomainException("An invitation must name an attendee.");
        }

        return new EventAttendee
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AttendeeId = attendeeId,
            OptInNotify = optInNotify,
            Status = AttendanceStatus.Pending,
            InvitedAt = DateTimeOffset.UtcNow,
        };
    }

    internal void Respond(AttendanceStatus response)
    {
        if (response == AttendanceStatus.Pending)
        {
            throw new DomainException("An attendee can accept or decline, but cannot revert to pending.");
        }

        Status = response;
        RespondedAt = DateTimeOffset.UtcNow;
    }
}
