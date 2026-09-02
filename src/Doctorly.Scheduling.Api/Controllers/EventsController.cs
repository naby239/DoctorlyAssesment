using Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;
using Doctorly.Scheduling.Application.Events.Dtos;
using Doctorly.Scheduling.Application.Events.Queries.GetEventById;
using Doctorly.Scheduling.Application.Events.Queries.ListEvents;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Doctorly.Scheduling.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Schedules a new event. Attendees are matched by email address and reused if already known.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> ScheduleEvent(
        ScheduleEventCommand command,
        CancellationToken cancellationToken)
    {
        var scheduled = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        SetETag(scheduled.Version);

        return CreatedAtAction(nameof(GetEvent), new { id = scheduled.Id }, scheduled);
    }

    /// <summary>
    /// Lists calendar events. Every filter is optional and they combine.
    /// </summary>
    /// <remarks>
    /// <c>from</c> and <c>to</c> select events overlapping that window, so an appointment that
    /// began before it is still returned. <c>search</c> matches the title or description.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventSummaryDto>>> ListEvents(
        [FromQuery] ListEventsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(query, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns a single event with its attendees. The ETag is required to update it.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(Guid id, CancellationToken cancellationToken)
    {
        var found = await sender.Send(new GetEventByIdQuery(id), cancellationToken).ConfigureAwait(false);

        if (found is null)
        {
            return NotFound();
        }

        SetETag(found.Version);

        return Ok(found);
    }

    private void SetETag(Guid version) => Response.Headers.ETag = $"\"{version}\"";
}
