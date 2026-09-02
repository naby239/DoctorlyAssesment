using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid Id) : IRequest<EventDto?>;
