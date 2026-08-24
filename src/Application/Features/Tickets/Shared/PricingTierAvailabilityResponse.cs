namespace Application.Features.Tickets.Shared;

public sealed record PricingTierAvailabilityResponse(
    Guid PricingTierId,
    string TierName,
    decimal Price);
