using Domain.Exceptions;

namespace Domain.Entities;

public sealed class PricingTier
{
    private PricingTier() { }

    private PricingTier(
        Guid id,
        Guid eventId,
        string name,
        decimal price)
    {
        Id = id;
        EventId = eventId;
        Name = name;
        Price = price;
    }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    internal static PricingTier Create(
        Guid eventId,
        string name,
        decimal price)
    {
        Validate(name, price);

        return new PricingTier(
            Guid.NewGuid(),
            eventId,
            name.Trim(),
            decimal.Round(price, 2, MidpointRounding.ToEven));
    }

    internal void Update(string name, decimal price)
    {
        Validate(name, price);

        Name = name.Trim();
        Price = decimal.Round(price, 2, MidpointRounding.ToEven);
    }

    private static void Validate(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw DomainErrors.PricingTier.NameRequired();
        }

        if (name.Trim().Length > 100)
        {
            throw DomainErrors.PricingTier.NameTooLong();
        }

        if (price <= 0)
        {
            throw DomainErrors.PricingTier.PriceMustBePositive();
        }
    }
}
