using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration;

public sealed class EventConfiguration
    : IEntityTypeConfiguration<Event>
{
    public void Configure(
        EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Venue)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EventDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EventTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(x => x.TotalCapacity)
            .IsRequired();

        builder.Property(x => x.SoldTickets)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasMany(x => x.PricingTiers)
        .WithOne()
        .HasForeignKey(x => x.EventId)
        .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.EventDate,
            x.EventTime
        });
    }
}