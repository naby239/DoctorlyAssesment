using Doctorly.Scheduling.Domain.Common;
using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Domain.Tests;

public sealed class CalendarEventTests
{
    private static readonly DateTimeOffset Monday9Am = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    private static CalendarEvent AnEvent() =>
        CalendarEvent.Schedule("Consultation", "Follow-up", Monday9Am, Monday9Am.AddHours(1));

    [Fact]
    public void Scheduling_an_event_starts_it_in_the_scheduled_state()
    {
        var calendarEvent = AnEvent();

        calendarEvent.Status.ShouldBe(EventStatus.Scheduled);
        calendarEvent.IsCancelled.ShouldBeFalse();
        calendarEvent.Attendees.ShouldBeEmpty();
        calendarEvent.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void An_event_must_end_after_it_starts()
    {
        var act = () => CalendarEvent.Schedule("Consultation", null, Monday9Am, Monday9Am.AddHours(-1));

        Should.Throw<DomainException>(act).Message.ShouldContain("end after it starts");
    }

    [Fact]
    public void An_event_cannot_start_and_end_at_the_same_moment()
    {
        var act = () => CalendarEvent.Schedule("Consultation", null, Monday9Am, Monday9Am);

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void Times_are_stored_in_utc()
    {
        var berlinMorning = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(2));

        var calendarEvent = CalendarEvent.Schedule("Consultation", null, berlinMorning, berlinMorning.AddHours(1));

        calendarEvent.StartTime.Kind.ShouldBe(DateTimeKind.Utc);
        calendarEvent.StartTime.Hour.ShouldBe(7);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_event_must_have_a_title(string? title)
    {
        var act = () => CalendarEvent.Schedule(title, null, Monday9Am, Monday9Am.AddHours(1));

        Should.Throw<DomainException>(act).Message.ShouldContain("title");
    }

    [Fact]
    public void A_title_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('a', SchedulingLimits.EventTitleMaxLength + 1);

        var act = () => CalendarEvent.Schedule(tooLong, null, Monday9Am, Monday9Am.AddHours(1));

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void A_title_exactly_at_the_limit_is_accepted()
    {
        var atLimit = new string('a', SchedulingLimits.EventTitleMaxLength);

        var calendarEvent = CalendarEvent.Schedule(atLimit, null, Monday9Am, Monday9Am.AddHours(1));

        calendarEvent.Title.Length.ShouldBe(SchedulingLimits.EventTitleMaxLength);
    }

    [Fact]
    public void A_description_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('a', SchedulingLimits.EventDescriptionMaxLength + 1);

        var act = () => CalendarEvent.Schedule("Consultation", tooLong, Monday9Am, Monday9Am.AddHours(1));

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void Cancelling_keeps_the_event_and_records_why()
    {
        var calendarEvent = AnEvent();

        calendarEvent.Cancel("Patient called to reschedule");

        calendarEvent.Status.ShouldBe(EventStatus.Cancelled);
        calendarEvent.CancellationReason.ShouldBe("Patient called to reschedule");
        calendarEvent.CancelledAt.ShouldNotBeNull();
    }

    [Fact]
    public void An_event_cannot_be_cancelled_twice()
    {
        var calendarEvent = AnEvent();
        calendarEvent.Cancel();

        Should.Throw<DomainException>(() => calendarEvent.Cancel());
    }

    [Fact]
    public void A_cancelled_event_cannot_be_rescheduled()
    {
        var calendarEvent = AnEvent();
        calendarEvent.Cancel();

        var act = () => calendarEvent.Reschedule(Monday9Am.AddDays(1), Monday9Am.AddDays(1).AddHours(1));

        Should.Throw<DomainException>(act).Message.ShouldContain("cancelled");
    }

    [Fact]
    public void A_cancelled_event_cannot_take_new_invitations()
    {
        var calendarEvent = AnEvent();
        calendarEvent.Cancel();

        Should.Throw<DomainException>(() => calendarEvent.Invite(Guid.NewGuid()));
    }

    [Fact]
    public void Rescheduling_moves_the_event()
    {
        var calendarEvent = AnEvent();
        var newStart = Monday9Am.AddDays(1);

        calendarEvent.Reschedule(newStart, newStart.AddHours(1));

        calendarEvent.StartTime.ShouldBe(newStart.UtcDateTime);
        calendarEvent.EndTime.ShouldBe(newStart.AddHours(1).UtcDateTime);
        calendarEvent.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Rescheduling_still_requires_the_end_to_follow_the_start()
    {
        var calendarEvent = AnEvent();

        var act = () => calendarEvent.Reschedule(Monday9Am, Monday9Am.AddHours(-1));

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void An_invitation_starts_as_pending()
    {
        var calendarEvent = AnEvent();

        var invitation = calendarEvent.Invite(Guid.NewGuid());

        invitation.Status.ShouldBe(AttendanceStatus.Pending);
        invitation.RespondedAt.ShouldBeNull();
        calendarEvent.Attendees.ShouldHaveSingleItem();
    }

    [Fact]
    public void The_same_attendee_cannot_be_invited_twice()
    {
        var calendarEvent = AnEvent();
        var attendeeId = Guid.NewGuid();
        calendarEvent.Invite(attendeeId);

        Should.Throw<DomainException>(() => calendarEvent.Invite(attendeeId))
            .Message.ShouldContain("already been invited");
    }

    [Theory]
    [InlineData(AttendanceStatus.Accepted)]
    [InlineData(AttendanceStatus.Declined)]
    public void Responding_records_the_answer_and_when_it_arrived(AttendanceStatus response)
    {
        var calendarEvent = AnEvent();
        var attendeeId = Guid.NewGuid();
        calendarEvent.Invite(attendeeId);

        calendarEvent.RespondToInvitation(attendeeId, response);

        var invitation = calendarEvent.Attendees.Single();
        invitation.Status.ShouldBe(response);
        invitation.RespondedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Someone_who_was_not_invited_cannot_respond()
    {
        var calendarEvent = AnEvent();

        var act = () => calendarEvent.RespondToInvitation(Guid.NewGuid(), AttendanceStatus.Accepted);

        Should.Throw<DomainException>(act).Message.ShouldContain("not invited");
    }

    [Fact]
    public void An_attendee_cannot_revert_to_pending()
    {
        var calendarEvent = AnEvent();
        var attendeeId = Guid.NewGuid();
        calendarEvent.Invite(attendeeId);

        var act = () => calendarEvent.RespondToInvitation(attendeeId, AttendanceStatus.Pending);

        Should.Throw<DomainException>(act);
    }
}
