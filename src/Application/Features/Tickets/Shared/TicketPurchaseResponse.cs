namespace Application.Features.Tickets.Purchase;

public sealed record TicketPurchaseResponse(
    Guid Id,
    Guid EventId,
    Guid PricingTierId,
    string PurchaserName,
    string PurchaserEmail,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    DateTime PurchasedAtUtc)
{
    public static TicketPurchaseResponse From(
        Domain.Entities.TicketPurchase purchase)
    {
        return new TicketPurchaseResponse(
            purchase.Id,
            purchase.EventId,
            purchase.PricingTierId,
            purchase.PurchaserName,
            purchase.PurchaserEmail,
            purchase.Quantity,
            purchase.UnitPrice,
            purchase.TotalAmount,
            purchase.PurchasedAtUtc);
    }
}