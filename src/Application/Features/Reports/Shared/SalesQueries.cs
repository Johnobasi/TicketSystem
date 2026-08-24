using Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports.Shared;

internal static class SalesQueries
{
    public static async Task<List<TierSalesRow>> GetSalesForEventsAsync(
        IApplicationDbContext db,
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return [];
        }

        return await (
            from purchase in db.TicketPurchases.AsNoTracking()
            join tier in db.PricingTiers.AsNoTracking()
                on purchase.PricingTierId equals tier.Id
            where eventIds.Contains(purchase.EventId)
            group purchase by new
            {
                purchase.EventId,
                purchase.PricingTierId,
                tier.Name
            }
            into sales
            select new TierSalesRow(
                sales.Key.EventId,
                sales.Key.PricingTierId,
                sales.Key.Name,
                sales.Sum(x => x.Quantity),
                sales.Sum(x => x.Quantity * x.UnitPrice)))
            .ToListAsync(cancellationToken);
    }
}
