using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Domain.Scheduling;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Commands.RespondToInvitation;

public sealed record RespondToInvitationCommand(
    Guid EventId,
    Guid AttendeeId,
    AttendanceStatus Response) : IRequest<EventDto?>;
