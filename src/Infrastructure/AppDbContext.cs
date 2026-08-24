using Application.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<PricingTier> PricingTiers => Set<PricingTier>();
    public DbSet<TicketPurchase> TicketPurchases => Set<TicketPurchase>();

    public void ClearTracking() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
