using Application.Features.Tickets.GetAvailability.Query;
using Application.Common;
using Application.Features.Tickets.Shared;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Application.Common.Abstraction.Handlers;

namespace Application.Features.Tickets.GetAvailability.Handler;

public sealed class GetTicketAvailabilityQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetTicketAvailabilityQuery, TicketAvailabilityResponse>
{
    public async Task<TicketAvailabilityResponse> Handle(
        GetTicketAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        var result = await db.Events
        .AsNoTracking()
        .Where(x => x.Id == query.EventId)
        .Select(x => new TicketAvailabilityResponse(
            x.Id,
            x.TotalCapacity,
            x.SoldTickets,
            x.RemainingCapacity,
            x.IsSoldOut))
        .SingleOrDefaultAsync(cancellationToken);

        return result
            ?? throw DomainErrors.Event.NotFound(
                query.EventId);
    }
}
