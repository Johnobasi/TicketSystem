using Api.Contracts.Events;
using Application.Common;
using Application.Common.Abstraction;
using Application.Features.Events.Create.Command;
using Application.Features.Events.Delete.Command;
using Application.Features.Events.GetById.Query;
using Application.Features.Events.Retrieve.Query;
using Application.Features.Events.Shared;
using Application.Features.Events.Update.Command;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(
    IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateEventCommand command,
        CancellationToken cancellationToken)
    {
        var id = await mediator.Send<Guid>(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventResponse>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventsQuery(
            page,
            pageSize);

        var result =
            await mediator.Send<PagedResult<EventResponse>>(
                query,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetEventByIdQuery(id);

        var result =
            await mediator.Send<EventResponse>(
                query,
                cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEventCommand(
            id,
            request.Name,
            request.Description,
            request.Venue,
            request.EventDate,
            request.EventTime,
            request.TotalCapacity);

        await mediator.Send(
            command,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteEventCommand(id),
            cancellationToken);

        return NoContent();
    }
}