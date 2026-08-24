using Application.Common;
using Application.Common.Abstraction;
using Application.Features.Reports.GetSalesSummary.Query;
using Application.Features.Reports.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(
    IMediator mediator) : ControllerBase
{
    [HttpGet("events/{eventId:guid}/sales")]
    [ProducesResponseType(typeof(PagedResult<SalesByEventResponse>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<SalesByEventResponse>>>
        GetSalesByEvent(
            Guid eventId,
            CancellationToken cancellationToken)
    {
        var query =
            new GetSalesByEventQuery(eventId);

        var result =
            await mediator.Send<PagedResult<SalesByEventResponse>>(
                query,
                cancellationToken);

        return Ok(result);
    }
}
