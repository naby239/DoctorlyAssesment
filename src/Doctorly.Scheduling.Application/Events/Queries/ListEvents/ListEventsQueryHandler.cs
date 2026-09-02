using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Queries.ListEvents;

public sealed class ListEventsQueryHandler(IEventQueries queries)
    : IRequestHandler<ListEventsQuery, PagedResult<EventSummaryDto>>
{
    public Task<PagedResult<EventSummaryDto>> Handle(
        ListEventsQuery request,
        CancellationToken cancellationToken) =>
        queries.ListAsync(request, cancellationToken);
}
