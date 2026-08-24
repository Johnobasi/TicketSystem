namespace Application.Features.Reports.Shared;

internal sealed record EventReportRow(
    Guid Id,
    string Name,
    DateOnly EventDate,
    TimeOnly EventTime,
    int TotalCapacity,
    int SoldTickets);

internal sealed record TierSalesRow(
    Guid EventId,
    Guid PricingTierId,
    string TierName,
    int QuantitySold,
    decimal Revenue);
