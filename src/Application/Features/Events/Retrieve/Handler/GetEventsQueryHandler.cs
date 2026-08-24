using Application.Common;
using Application.Common.Abstraction.Handlers;
using Application.Features.Events.Retrieve.Query;
using Application.Features.Events.Shared;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Events.Retrieve.Handler;

public sealed class GetEventsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetEventsQuery, PagedResult<EventResponse>>
{
    public async Task<PagedResult<EventResponse>> Handle(
        GetEventsQuery query,
        CancellationToken cancellationToken)
    {
        var totalCount = await db.Events.CountAsync(cancellationToken);

        var items = await db.Events
            .AsNoTracking()
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.EventTime)
            .ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new EventResponse(
                x.Id,
                x.Name,
                x.Description,
                x.Venue,
                x.EventDate,
                x.EventTime,
                x.TotalCapacity,
                x.SoldTickets,
                x.RemainingCapacity,
                x.IsSoldOut,
                x.PricingTiers
                    .OrderBy(t => t.Name)
                    .Select(t => new PricingTierResponse(
                        t.Id,
                        t.Name,
                        t.Price))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<EventResponse>(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }
}
