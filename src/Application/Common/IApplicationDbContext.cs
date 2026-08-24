using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common;

public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }
    DbSet<PricingTier> PricingTiers { get; }
    DbSet<TicketPurchase> TicketPurchases { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearTracking();
}
