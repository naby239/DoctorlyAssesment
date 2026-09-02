using Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;
using Doctorly.Scheduling.Application.Events.Dtos;
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

        Response.Headers.ETag = $"\"{scheduled.Version}\"";

        return Created($"/api/events/{scheduled.Id}", scheduled);
    }
}
