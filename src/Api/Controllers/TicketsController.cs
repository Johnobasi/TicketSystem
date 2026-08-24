using Api.Contracts.Tickets;
using Application.Common.Abstraction;
using Application.Common.Idempotent;
using Application.Features.Tickets.GetAvailability.Query;
using Application.Features.Tickets.Purchase;
using Application.Features.Tickets.Purchase.Command;
using Application.Features.Tickets.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}")]
public sealed class TicketsController(
    IMediator mediator) : ControllerBase
{
    [HttpPost("purchases")]
    public async Task<ActionResult<TicketPurchaseResponse>> Purchase(
    Guid eventId,
    PurchaseTicketsRequest request,
    [FromHeader(Name = "Idempotency-Key")]
    string idempotencyKey,
    CancellationToken cancellationToken)
    {
        var command = new PurchaseTicketsCommand(
            eventId,
            request.PricingTierId,
            request.Quantity,
            idempotencyKey,
            request.PurchaserName,
            request.PurchaserEmail);

        var result =
            await mediator.Send<
                IdempotentResult<TicketPurchaseResponse>>(
                    command,
                    cancellationToken);

        if (result.IsReplay)
        {
            return Ok(result.Value);
        }

        return Created(
            $"/api/events/{eventId}/purchases/{result.Value.Id}",
            result.Value);
    }

    [HttpGet("availability")]
    [ProducesResponseType(
        typeof(TicketAvailabilityResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketAvailabilityResponse>>
        GetAvailability(
            Guid eventId,
            CancellationToken cancellationToken)
    {
        var query =
            new GetTicketAvailabilityQuery(eventId);

        var result =
            await mediator.Send<TicketAvailabilityResponse>(
                query,
                cancellationToken);

        return Ok(result);
    }
}