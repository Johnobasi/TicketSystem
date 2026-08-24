namespace Application.Features.Reports.Shared;

public sealed record TierSalesSummary(
    Guid PricingTierId,
    string TierName,
    int QuantitySold,
    decimal Revenue);
