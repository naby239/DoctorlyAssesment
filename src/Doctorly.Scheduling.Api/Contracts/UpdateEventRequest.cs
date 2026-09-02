namespace Doctorly.Scheduling.Api.Contracts;

public sealed record UpdateEventRequest(
    string? Title,
    string? Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);
