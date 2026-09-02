using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;
using Doctorly.Scheduling.Application.Notifications;
using Doctorly.Scheduling.Domain.Common;
using Doctorly.Scheduling.Domain.Scheduling;
using NSubstitute;

namespace Doctorly.Scheduling.Application.Tests;

public sealed class ScheduleEventCommandHandlerTests
{
    private static readonly DateTimeOffset Monday9Am = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    private readonly ICalendarEventRepository _events = Substitute.For<ICalendarEventRepository>();
    private readonly IAttendeeRepository _attendees = Substitute.For<IAttendeeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();

    private ScheduleEventCommandHandler CreateHandler() => new(_events, _attendees, _unitOfWork, _notifications);

    private static ScheduleEventCommand ACommand(params ScheduleEventAttendee[] attendees) =>
        new("Consultation", "Follow-up", Monday9Am, Monday9Am.AddHours(1), attendees);

    [Fact]
    public async Task Scheduling_an_event_persists_it_and_commits()
    {
        var result = await CreateHandler().Handle(ACommand(), CancellationToken.None);

        result.Title.ShouldBe("Consultation");
        result.Status.ShouldBe(nameof(EventStatus.Scheduled));
        result.Version.ShouldNotBe(Guid.Empty);

        await _events.Received(1).AddAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scheduling_notifies_the_attendees_who_opted_in()
    {
        _attendees.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Attendee?)null);

        await CreateHandler().Handle(
            ACommand(
                new ScheduleEventAttendee("Anna Weber", "anna@practice.de", null),
                new ScheduleEventAttendee("Dr Klein", "klein@practice.de", null, OptInNotify: false)),
            CancellationToken.None);

        await _notifications.Received(1).DispatchAsync(
            Arg.Is<EventNotification>(n =>
                n.Kind == NotificationKind.EventScheduled
                && n.Recipients.Count == 1
                && n.Recipients[0].Email == "anna@practice.de"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_event_that_fails_to_save_notifies_nobody()
    {
        var command = new ScheduleEventCommand(
            "Consultation", null, Monday9Am, Monday9Am.AddHours(-1), []);

        await Should.ThrowAsync<DomainException>(
            () => CreateHandler().Handle(command, CancellationToken.None));

        await _notifications.DidNotReceive().DispatchAsync(
            Arg.Any<EventNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_attendee_is_registered()
    {
        _attendees.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Attendee?)null);

        var result = await CreateHandler().Handle(
            ACommand(new ScheduleEventAttendee("Anna Weber", "anna@practice.de", null)),
            CancellationToken.None);

        await _attendees.Received(1).AddAsync(Arg.Any<Attendee>(), Arg.Any<CancellationToken>());

        var invited = result.Attendees.ShouldHaveSingleItem();
        invited.Email.ShouldBe("anna@practice.de");
        invited.Status.ShouldBe(nameof(AttendanceStatus.Pending));
    }

    [Fact]
    public async Task A_known_attendee_is_reused_rather_than_duplicated()
    {
        var existing = Attendee.Register("Anna Weber", "anna@practice.de");

        _attendees.FindByEmailAsync("anna@practice.de", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await CreateHandler().Handle(
            ACommand(new ScheduleEventAttendee("Anna W", "ANNA@practice.de", null)),
            CancellationToken.None);

        await _attendees.DidNotReceive().AddAsync(Arg.Any<Attendee>(), Arg.Any<CancellationToken>());
        result.Attendees.ShouldHaveSingleItem().AttendeeId.ShouldBe(existing.Id);
    }

    [Fact]
    public async Task The_attendee_lookup_uses_the_normalised_address()
    {
        _attendees.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Attendee?)null);

        await CreateHandler().Handle(
            ACommand(new ScheduleEventAttendee("Anna Weber", "  ANNA@Practice.DE  ", null)),
            CancellationToken.None);

        await _attendees.Received(1).FindByEmailAsync("anna@practice.de", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_notification_preference_is_carried_onto_the_invitation()
    {
        _attendees.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Attendee?)null);

        var result = await CreateHandler().Handle(
            ACommand(new ScheduleEventAttendee("Anna Weber", "anna@practice.de", null, OptInNotify: false)),
            CancellationToken.None);

        result.Attendees.ShouldHaveSingleItem().OptInNotify.ShouldBeFalse();
    }

    [Fact]
    public async Task An_invalid_event_is_not_persisted()
    {
        var command = new ScheduleEventCommand(
            "Consultation", null, Monday9Am, Monday9Am.AddHours(-1), []);

        await Should.ThrowAsync<DomainException>(
            () => CreateHandler().Handle(command, CancellationToken.None));

        await _events.DidNotReceive().AddAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
