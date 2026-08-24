using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration;

public sealed class PricingTierConfiguration : IEntityTypeConfiguration<PricingTier>
{
    public void Configure(
        EntityTypeBuilder<PricingTier> builder)
    {
        builder.ToTable("PricingTiers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.EventId,
            x.Name
        })
        .IsUnique();
    }
}