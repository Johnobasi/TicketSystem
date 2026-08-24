using Application.Common;
using Application.Features.Events.Shared;
using Application.Features.Events.GetById.Query;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Application.Common.Abstraction.Handlers;

namespace Application.Features.Events.GetById.Handler;

public sealed class GetEventByIdQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetEventByIdQuery, EventResponse>
{
    public async Task<EventResponse> Handle(
        GetEventByIdQuery query,
        CancellationToken cancellationToken)
    {
        var response = await db.Events
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
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
            .SingleOrDefaultAsync(cancellationToken);

        return response
            ?? throw DomainErrors.Event.NotFound(query.Id);
    }
}
