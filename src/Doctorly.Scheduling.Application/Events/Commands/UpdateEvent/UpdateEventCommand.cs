using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand(
    Guid Id,
    Guid ExpectedVersion,
    string? Title,
    string? Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime) : IRequest<EventDto?>;
