namespace Api.Contracts.Tickets;

public sealed record PurchaseTicketsRequest(
    Guid PricingTierId,
    int Quantity,
    string PurchaserName,
    string PurchaserEmail);
