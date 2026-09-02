using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;

public sealed record ScheduleEventCommand(
    string? Title,
    string? Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyList<ScheduleEventAttendee>? Attendees) : IRequest<EventDto>;

public sealed record ScheduleEventAttendee(
    string? Name,
    string? Email,
    string? ContactNumber,
    bool OptInNotify = true);
