namespace Doctorly.Scheduling.Application.Events.Dtos;

public sealed record EventSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    Guid Version,
    int AttendeeCount);
