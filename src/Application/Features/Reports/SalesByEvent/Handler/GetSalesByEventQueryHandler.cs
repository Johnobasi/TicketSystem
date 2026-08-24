using Application.Common;
using Application.Common.Abstraction.Handlers;
using Application.Features.Reports.GetSalesSummary.Query;
using Application.Features.Reports.Shared;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports.GetSalesSummary.Handler;

public sealed class GetSalesByEventQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetSalesByEventQuery, PagedResult<SalesByEventResponse>>
{
    public async Task<PagedResult<SalesByEventResponse>> Handle(
        GetSalesByEventQuery query,
        CancellationToken cancellationToken)
    {
        var exists = await db.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == query.EventId,
                cancellationToken);

        if (!exists)
        {
            throw DomainErrors.Event.NotFound(
                query.EventId);
        }

        var result = await db.TicketPurchases
            .AsNoTracking()
            .Where(x => x.EventId == query.EventId)
            .GroupBy(x => x.EventId)
            .Select(g => new SalesByEventResponse(
                g.Key,
                g.Sum(x => x.Quantity),
                g.Sum(x =>
                    x.Quantity * x.UnitPrice)))
            .SingleOrDefaultAsync(
                cancellationToken);


        return new PagedResult<SalesByEventResponse>(
            new List<SalesByEventResponse>
            {
                result ?? new SalesByEventResponse(query.EventId, 0, 0m)
            },
            1,
            1,
            1);
        }
}
