using Domain.Entities;
using Domain.Exceptions;
using Domain.Models;

namespace TicketSystemTests;

public sealed class EventTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly FutureEventDate =
     new(2026, 3, 1);

    private static readonly TimeOnly FutureEventTime =
        new(19, 0);

    private static IReadOnlyCollection<PricingTierDefinition> Tiers =>
    [
        new("General", 25m),
    new("VIP", 100m)
    ];

    private static Event CreateEvent(int capacity = 100) =>
        Event.Create(
            "Summer Fest",
            "An outdoor festival",
            "Hyde Park",
            FutureEventDate,
            FutureEventTime,
            capacity,
            Tiers,
            Now);

    [Fact]
    public void Create_creates_event_with_pricing_tiers()
    {
        var @event = CreateEvent();

        Assert.NotEqual(Guid.Empty, @event.Id);
        Assert.Equal("Summer Fest", @event.Name);
        Assert.Equal(100, @event.TotalCapacity);
        Assert.Equal(0, @event.SoldTickets);
        Assert.Equal(100, @event.RemainingCapacity);
        Assert.False(@event.IsSoldOut);
        Assert.Equal(2, @event.PricingTiers.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_missing_name(string name)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Event.Create(name, "Description", "Venue", FutureEventDate, FutureEventTime, 100, Tiers, Now));

        Assert.Equal("Event.NameRequired", exception.Code);
    }

    [Fact]
    public void Create_rejects_non_positive_capacity()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Event.Create("Name", "Description", "Venue", FutureEventDate, FutureEventTime, 0, Tiers, Now));

        Assert.Equal("Event.CapacityMustBePositive", exception.Code);
    }

    [Fact]
    public void Create_rejects_event_in_the_past()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Event.Create(
                "Name",
                "Description",
                "Venue",
                new DateOnly(2025, 12, 31),
                new TimeOnly(23, 59),
                100,
                Tiers,
                Now));

        Assert.Equal("Event.DateMustBeInFuture", exception.Code);
    }

    [Fact]
    public void Create_requires_at_least_one_pricing_tier()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Event.Create("Name", "Description", "Venue", FutureEventDate, FutureEventTime, 100, [], Now));

        Assert.Equal("PricingTier.AtLeastOneRequired", exception.Code);
    }

    [Fact]
    public void Create_rejects_duplicate_tier_names_case_insensitively()
    {
        var tiers = new PricingTierDefinition[]
        {
            new("General", 25m),
            new("general", 30m)
        };

        var exception = Assert.Throws<DomainValidationException>(() =>
            Event.Create("Name", "Description", "Venue", FutureEventDate, FutureEventTime, 100, tiers, Now));

        Assert.Equal("PricingTier.DuplicateName", exception.Code);
    }

    [Fact]
    public void Create_rejects_non_positive_tier_price()
    {
        var tiers = new PricingTierDefinition[]
        {
            new("General", 0m)
        };

        var exception = Assert.Throws<DomainValidationException>(() =>
            Event.Create("Name", "Description", "Venue", FutureEventDate, FutureEventTime, 100, tiers, Now));

        Assert.Equal("PricingTier.PriceMustBePositive", exception.Code);
    }

    [Fact]
    public void Purchase_increases_sold_tickets_and_uses_tier_price()
    {
        var @event = CreateEvent();
        var tier = @event.PricingTiers.Single(x => x.Name == "VIP");

        var purchase = @event.PurchaseTickets(
            tier.Id,
            3,
            "key-1",
            "fingerprint-1",
            "Alice",
            "alice@example.com",
            Now);

        Assert.Equal(3, purchase.Quantity);
        Assert.Equal(100m, purchase.UnitPrice);
        Assert.Equal(300m, purchase.TotalAmount);
        Assert.Equal(3, @event.SoldTickets);
        Assert.Equal(97, @event.RemainingCapacity);
    }

    [Fact]
    public void Purchase_increments_concurrency_version()
    {
        var @event = CreateEvent();
        var tierId = @event.PricingTiers.First().Id;

        @event.PurchaseTickets(
            tierId, 1, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now);

        Assert.Equal(1, @event.Version);
    }

    [Fact]
    public void Purchase_rejects_when_event_capacity_is_exceeded()
    {
        var @event = CreateEvent(capacity: 5);
        var tierId = @event.PricingTiers.First().Id;

        @event.PurchaseTickets(
            tierId, 4, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now);

        var exception = Assert.Throws<ConflictException>(() =>
            @event.PurchaseTickets(
                tierId, 2, "key-2", "fingerprint-2", "Bob", "bob@example.com", Now));

        Assert.Equal("TicketPurchase.InsufficientCapacity", exception.Code);
        Assert.Equal(4, @event.SoldTickets);
    }

    [Fact]
    public void Purchase_can_exactly_fill_event_capacity()
    {
        var @event = CreateEvent(capacity: 5);
        var tierId = @event.PricingTiers.First().Id;

        @event.PurchaseTickets(
            tierId, 5, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now);

        Assert.True(@event.IsSoldOut);
        Assert.Equal(0, @event.RemainingCapacity);
    }

    [Fact]
    public void Purchase_rejects_unknown_pricing_tier()
    {
        var @event = CreateEvent();

        var exception = Assert.Throws<NotFoundException>(() =>
            @event.PurchaseTickets(
                Guid.NewGuid(), 1, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now));

        Assert.Equal("PricingTier.NotFound", exception.Code);
    }

    [Fact]
    public void Purchase_rejects_quantity_above_limit()
    {
        var @event = CreateEvent(1000);
        var tierId = @event.PricingTiers.First().Id;

        var exception = Assert.Throws<DomainValidationException>(() =>
            @event.PurchaseTickets(
                tierId, 51, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now));

        Assert.Equal("TicketPurchase.QuantityExceedsMaxPerPurchase", exception.Code);
    }

    [Fact]
    public void Purchase_rejects_missing_request_fingerprint()
    {
        var @event = CreateEvent();
        var tierId = @event.PricingTiers.First().Id;

        var exception = Assert.Throws<DomainValidationException>(() =>
            @event.PurchaseTickets(
                tierId, 1, "key-1", "", "Alice", "alice@example.com", Now));

        Assert.Equal("TicketPurchase.RequestFingerprintRequired", exception.Code);
    }

    [Fact]
    public void Purchase_rejects_missing_idempotency_key()
    {
        var @event = CreateEvent();
        var tierId = @event.PricingTiers.First().Id;

        var exception = Assert.Throws<DomainValidationException>(() =>
            @event.PurchaseTickets(
                tierId, 1, "", "fingerprint-1", "Alice", "alice@example.com", Now));

        Assert.Equal("TicketPurchase.IdempotencyKeyRequired", exception.Code);
    }

    [Fact]
    public void Purchase_rejects_invalid_email()
    {
        var @event = CreateEvent();
        var tierId = @event.PricingTiers.First().Id;

        var exception = Assert.Throws<DomainValidationException>(() =>
            @event.PurchaseTickets(
                tierId, 1, "key-1", "fingerprint-1", "Alice", "invalid", Now));

        Assert.Equal("TicketPurchase.PurchaserEmailInvalid", exception.Code);
    }

    [Fact]
    public void Update_details_allows_non_disruptive_changes_after_sales()
    {
        var @event = CreateEvent(10);
        var tierId = @event.PricingTiers.First().Id;
        @event.PurchaseTickets(tierId, 2, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now);

        @event.UpdateDetails(
            "Renamed",
            "Updated description",
            "Updated venue",
            FutureEventDate,
            FutureEventTime,
            10,
            Now.AddDays(1));

        Assert.Equal("Renamed", @event.Name);
        Assert.Equal("Updated description", @event.Description);
    }

    [Fact]
    public void Update_details_rejects_capacity_below_sold_tickets()
    {
        var @event = CreateEvent(10);
        var tierId = @event.PricingTiers.First().Id;
        @event.PurchaseTickets(tierId, 5, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now);

        var exception = Assert.Throws<DomainValidationException>(() =>
            @event.UpdateDetails(
                @event.Name,
                @event.Description,
                @event.Venue,
                @event.EventDate,
                @event.EventTime,
                4,
                Now));

        Assert.Equal("Event.CapacityBelowSoldTickets", exception.Code);
    }

    [Fact]
    public void Update_pricing_tier_changes_name_and_price()
    {
        var @event = CreateEvent();
        var tier = @event.PricingTiers.First();

        @event.UpdatePricingTier(tier.Id, "Standard", 35m);

        Assert.Equal("Standard", tier.Name);
        Assert.Equal(35m, tier.Price);
    }

    [Fact]
    public void Update_pricing_tier_rejects_duplicate_name()
    {
        var @event = CreateEvent();
        var tier = @event.PricingTiers.First(x => x.Name == "General");

        var exception = Assert.Throws<DomainValidationException>(() =>
            @event.UpdatePricingTier(tier.Id, "VIP", 35m));

        Assert.Equal("PricingTier.DuplicateName", exception.Code);
    }

    [Fact]
    public void Delete_is_allowed_before_sales()
    {
        var @event = CreateEvent();
        var exception = Record.Exception(@event.EnsureCanBeDeleted);
        Assert.Null(exception);
    }

    [Fact]
    public void Delete_is_rejected_after_sales()
    {
        var @event = CreateEvent();
        var tierId = @event.PricingTiers.First().Id;
        @event.PurchaseTickets(tierId, 1, "key-1", "fingerprint-1", "Alice", "alice@example.com", Now);

        var exception = Assert.Throws<ConflictException>(@event.EnsureCanBeDeleted);
        Assert.Equal("Event.HasSoldTickets", exception.Code);
    }
}
