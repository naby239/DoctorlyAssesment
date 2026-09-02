namespace Doctorly.Scheduling.Application.Events.Dtos;

public sealed record EventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    Guid Version,
    IReadOnlyList<EventAttendeeDto> Attendees);

public sealed record EventAttendeeDto(
    Guid AttendeeId,
    string Name,
    string Email,
    string Status,
    bool OptInNotify);
