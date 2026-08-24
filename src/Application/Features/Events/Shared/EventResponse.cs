namespace Application.Features.Events.Shared;

public sealed record EventResponse(
    Guid Id,
    string Name,
    string Description,
    string Venue,
    DateOnly EventDate,
    TimeOnly EventTime,
    int TotalCapacity,
    int SoldTickets,
    int RemainingCapacity,
    bool IsSoldOut,
    IReadOnlyCollection<PricingTierResponse> PricingTiers);

public sealed record PricingTierResponse(
    Guid Id,
    string Name,
    decimal Price);
