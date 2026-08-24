namespace Domain.Entities;

public sealed class TicketPurchase
{
    private TicketPurchase() { }

    public Guid Id { get; private set; }
   
    public Guid EventId { get; private set; }
    public Guid PricingTierId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public string PurchaserName { get; private set; } = string.Empty;
    public string PurchaserEmail { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount => UnitPrice * Quantity;
    public DateTime PurchasedAtUtc { get; private set; }

    internal static TicketPurchase Create(
        Guid eventId,
        Guid pricingTierId,
        string idempotencyKey,
        string requestFingerprint,
        string purchaserName,
        string purchaserEmail,
        int quantity,
        decimal unitPrice,
        DateTime purchasedAtUtc)
    {
        return new TicketPurchase
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            PricingTierId = pricingTierId,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = requestFingerprint,
            PurchaserName = purchaserName,
            PurchaserEmail = purchaserEmail,
            Quantity = quantity,
            UnitPrice = unitPrice,
            PurchasedAtUtc = purchasedAtUtc
        };
    }
}
