using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Application.Events.Queries.ListEvents;
using Doctorly.Scheduling.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Doctorly.Scheduling.Infrastructure.Persistence.Queries;

internal sealed class EventQueries(SchedulingDbContext context) : IEventQueries
{
    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await context.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartTime,
                e.EndTime,
                e.Status,
                e.CancellationReason,
                e.CancelledAt,
                e.Version,
                Attendees = e.Attendees
                    .Join(
                        context.Attendees,
                        invitation => invitation.AttendeeId,
                        attendee => attendee.Id,
                        (invitation, attendee) => new
                        {
                            invitation.AttendeeId,
                            attendee.Name,
                            attendee.Email,
                            invitation.Status,
                            invitation.OptInNotify,
                        })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (found is null)
        {
            return null;
        }

        return new EventDto(
            found.Id,
            found.Title,
            found.Description,
            found.StartTime,
            found.EndTime,
            found.Status.ToString(),
            found.CancellationReason,
            found.CancelledAt,
            found.Version,
            found.Attendees
                .Select(a => new EventAttendeeDto(
                    a.AttendeeId,
                    a.Name,
                    a.Email,
                    a.Status.ToString(),
                    a.OptInNotify))
                .ToList());
    }

    public async Task<PagedResult<EventSummaryDto>> ListAsync(
        ListEventsQuery query,
        CancellationToken cancellationToken)
    {
        var events = context.Events.AsNoTracking();

        // Overlap, not containment: a calendar view wants anything touching the window,
        // including an appointment that started before it.
        if (query.From.HasValue)
        {
            var from = query.From.Value.UtcDateTime;
            events = events.Where(e => e.EndTime > from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value.UtcDateTime;
            events = events.Where(e => e.StartTime < to);
        }

        if (query.Status.HasValue)
        {
            events = events.Where(e => e.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AttendeeEmail))
        {
            var email = Attendee.NormaliseEmail(query.AttendeeEmail);

            var attendeeIds = context.Attendees
                .Where(a => a.Email == email)
                .Select(a => a.Id);

            events = events.Where(e => e.Attendees.Any(ea => attendeeIds.Contains(ea.AttendeeId)));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{Escape(query.Search.Trim())}%";

            events = events.Where(e =>
                EF.Functions.Like(e.Title, pattern)
                || (e.Description != null && EF.Functions.Like(e.Description, pattern)));
        }

        var totalCount = await events.CountAsync(cancellationToken).ConfigureAwait(false);

        // Id as a tiebreak, otherwise two events at the same time can shuffle between pages.
        var page = await events
            .OrderBy(e => e.StartTime)
            .ThenBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartTime,
                e.EndTime,
                e.Status,
                e.Version,
                AttendeeCount = e.Attendees.Count,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = page
            .Select(e => new EventSummaryDto(
                e.Id,
                e.Title,
                e.Description,
                e.StartTime,
                e.EndTime,
                e.Status.ToString(),
                e.Version,
                e.AttendeeCount))
            .ToList();

        return new PagedResult<EventSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }

    // A search for "50%" should not match everything.
    private static string Escape(string term) =>
        term.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
