using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.CancelEvent;

public sealed record CancelEventCommand(
    Guid Id,
    Guid ExpectedVersion,
    string? Reason) : IRequest<EventDto?>;
