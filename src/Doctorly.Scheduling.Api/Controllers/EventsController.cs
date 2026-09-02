using Doctorly.Scheduling.Api.Contracts;
using Doctorly.Scheduling.Application.Events.Commands.CancelEvent;
using Doctorly.Scheduling.Application.Events.Commands.RespondToInvitation;
using Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;
using Doctorly.Scheduling.Application.Events.Commands.UpdateEvent;
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

    /// <summary>
    /// Updates an event. Requires the If-Match header carrying the ETag from a previous read.
    /// </summary>
    /// <remarks>
    /// Returns 428 when If-Match is absent and 412 when it no longer matches, so a caller can
    /// never silently overwrite a change made by someone else.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<EventDto>> UpdateEvent(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryReadIfMatch(out var expectedVersion, out var failure))
        {
            return failure;
        }

        var command = new UpdateEventCommand(
            id,
            expectedVersion,
            request.Title,
            request.Description,
            request.StartTime,
            request.EndTime);

        var updated = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (updated is null)
        {
            return NotFound();
        }

        SetETag(updated.Version);

        return Ok(updated);
    }

    /// <summary>
    /// Cancels an event. Requires the If-Match header carrying the ETag from a previous read.
    /// </summary>
    /// <remarks>
    /// The event is not removed. It moves to the Cancelled state, keeping its attendees and its
    /// history, and is still returned by the list endpoint under <c>status=Cancelled</c>.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<EventDto>> CancelEvent(
        Guid id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        if (!TryReadIfMatch(out var expectedVersion, out var failure))
        {
            return failure;
        }

        var cancelled = await sender
            .Send(new CancelEventCommand(id, expectedVersion, reason), cancellationToken)
            .ConfigureAwait(false);

        if (cancelled is null)
        {
            return NotFound();
        }

        SetETag(cancelled.Version);

        return Ok(cancelled);
    }

    /// <summary>
    /// Records an attendee accepting or declining their invitation.
    /// </summary>
    /// <remarks>
    /// No If-Match here. An attendee replying to an invitation cannot conflict with a change to
    /// the event's title or time, and typically follows a link without ever having read an ETag.
    /// </remarks>
    [HttpPost("{id:guid}/attendees/{attendeeId:guid}/response")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> RespondToInvitation(
        Guid id,
        Guid attendeeId,
        RespondToInvitationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RespondToInvitationCommand(id, attendeeId, request.Response);

        var updated = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (updated is null)
        {
            return NotFound();
        }

        SetETag(updated.Version);

        return Ok(updated);
    }

    private bool TryReadIfMatch(out Guid version, out ActionResult failure)
    {
        version = Guid.Empty;
        failure = null!;

        var header = Request.Headers.IfMatch.ToString();

        if (string.IsNullOrWhiteSpace(header))
        {
            failure = Problem(
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "If-Match is required.",
                detail: "Read the event first and send its ETag as If-Match, so a concurrent change cannot be overwritten.");

            return false;
        }

        // Strip the quotes an ETag is wrapped in, and the weak validator prefix if present.
        var candidate = header.Trim();

        if (candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[2..];
        }

        candidate = candidate.Trim('"');

        if (!Guid.TryParse(candidate, out version))
        {
            failure = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "If-Match is not a valid ETag.",
                detail: "Send the ETag exactly as it was returned, for example: If-Match: \"<guid>\".");

            return false;
        }

        return true;
    }

    private void SetETag(Guid version) => Response.Headers.ETag = $"\"{version}\"";
}
