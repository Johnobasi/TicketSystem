namespace Api.Contracts.Events;

public sealed record UpdateEventRequest(
    string Name,
    string Description,
    string Venue,
    DateOnly EventDate,
    TimeOnly EventTime,
    int TotalCapacity,
    IReadOnlyCollection<UpdatePricingTierRequest> PricingTiers);

public sealed record UpdatePricingTierRequest(
    Guid Id,
    string Name,
    decimal Price);
