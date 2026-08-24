using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TicketPurchaseConfiguration
    : IEntityTypeConfiguration<TicketPurchase>
{
    public void Configure(
        EntityTypeBuilder<TicketPurchase> builder)
    {
        builder.ToTable("TicketPurchases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.PricingTierId)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RequestFingerprint)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.PurchaserName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PurchaserEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PurchasedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.EventId,
            x.IdempotencyKey
        })
        .IsUnique();

        builder.HasOne<Event>()
        .WithMany()
        .HasForeignKey(x => x.EventId)
        .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.PricingTierId);
    }
}