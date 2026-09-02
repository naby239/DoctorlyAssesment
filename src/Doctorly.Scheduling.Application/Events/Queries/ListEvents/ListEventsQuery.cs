using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Domain.Scheduling;
using MediatR;

namespace Doctorly.Scheduling.Application.Events.Queries.ListEvents;

public sealed record ListEventsQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    EventStatus? Status = null,
    string? AttendeeEmail = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<EventSummaryDto>>;
