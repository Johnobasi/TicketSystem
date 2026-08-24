namespace Application.Features.Tickets.Purchase.Command;

public sealed record PurchaseTicketsCommand(
     Guid EventId,
    Guid PricingTierId,
    int Quantity,
    string IdempotencyKey,
    string PurchaserName,
    string PurchaserEmail);
