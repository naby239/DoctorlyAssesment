using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Queries.GetEventById;

public sealed class GetEventByIdQueryHandler(IEventQueries queries)
    : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    public Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken) =>
        queries.GetByIdAsync(request.Id, cancellationToken);
}
