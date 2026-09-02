using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Application.Events.Queries.ListEvents;

namespace Doctorly.Scheduling.Application.Common.Interfaces;

// Read side. Commands go through the aggregate so the rules are enforced; queries project
// straight to DTOs, because a read has no invariant to protect.
public interface IEventQueries
{
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<EventSummaryDto>> ListAsync(ListEventsQuery query, CancellationToken cancellationToken);
}
