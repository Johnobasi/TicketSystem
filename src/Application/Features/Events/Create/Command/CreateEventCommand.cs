namespace Application.Features.Events.Create.Command;

public sealed record CreatePricingTierRequest(
    string Name,
    decimal Price);

public sealed record CreateEventCommand(
    string Name,
    string Description,
    string Venue,
    DateOnly EventDate,
    TimeOnly EventTime,
    int TotalCapacity,
    IReadOnlyCollection<CreatePricingTierRequest> PricingTiers);
