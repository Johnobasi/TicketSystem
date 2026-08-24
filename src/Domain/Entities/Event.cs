using System.Net.Mail;
using Domain.Exceptions;
using Domain.Models;

namespace Domain.Entities;

public sealed class Event
{
    private readonly List<PricingTier> _pricingTiers = [];

    private Event() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Venue { get; private set; } = string.Empty;
    public DateOnly EventDate { get; private set; }
    public TimeOnly EventTime { get; private set; }
    public int TotalCapacity { get; private set; }
    public int SoldTickets { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PricingTier> PricingTiers => _pricingTiers.AsReadOnly();

    public int RemainingCapacity => TotalCapacity - SoldTickets;
    public bool IsSoldOut => RemainingCapacity == 0;

    //create
    public static Event Create(
        string name,
        string description,
        string venue,
        DateOnly eventDate,
        TimeOnly eventTime,
        int totalCapacity,
        IReadOnlyCollection<PricingTierDefinition> pricingTiers,
        DateTime nowUtc)
    {
        ValidateDetails(
             name,
             description,
             venue,
             eventDate,
             eventTime);

        ValidateCapacity(totalCapacity);

        ValidatePricingTiers(pricingTiers);

        ValidateFutureEventDateTime(
            eventDate,
            eventTime,
            nowUtc);

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description.Trim(),
            Venue = venue.Trim(),
            EventDate = eventDate,
            EventTime = eventTime,
            TotalCapacity = totalCapacity,
            CreatedAtUtc = nowUtc,
            Version = 0
        };

        foreach (var tier in pricingTiers)
        {
            @event._pricingTiers.Add(
                PricingTier.Create(
                    @event.Id,
                    tier.Name,
                    tier.Price));
        }

        return @event;
    }

    public void UpdateDetails(
        string name,
        string description,
        string venue,
        DateOnly eventDate,
        TimeOnly eventTime,
        int totalCapacity,
        DateTime nowUtc)
    {
        ValidateDetails(name, description, venue, eventDate, eventTime);
        ValidateCapacity(totalCapacity);

        if (totalCapacity < SoldTickets)
        {
            throw DomainErrors.Event.CapacityBelowSoldTickets(
                SoldTickets);
        }

        ValidateFutureEventDateTime(
            eventDate,
            eventTime,
            nowUtc);

        Name = name.Trim();
        Description = description.Trim();
        Venue = venue.Trim();
        EventTime = eventTime;
        EventDate = eventDate;
        TotalCapacity = totalCapacity;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void UpdatePricingTier(
        Guid pricingTierId,
        string name,
        decimal price)
    {
        var tier = _pricingTiers.FirstOrDefault(x => x.Id == pricingTierId)
            ?? throw DomainErrors.PricingTier.NotFound(pricingTierId);

        ValidatePricingTier(name, price);
        var duplicate = _pricingTiers.Any(
            x => x.Id != pricingTierId &&
                 x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw DomainErrors.PricingTier.DuplicateName(name);
        }

        tier.Update(name, price);
        Version++;
    }
    public TicketPurchase PurchaseTickets(
        Guid pricingTierId,
        int quantity,
        string idempotencyKey,
        string requestFingerprint,
        string purchaserName,
        string purchaserEmail,
        DateTime purchasedAtUtc)
    {
        ValidatePurchase(
           quantity,
           idempotencyKey,
           requestFingerprint,
           purchaserName,
           purchaserEmail);

        var tier = _pricingTiers.FirstOrDefault(x => x.Id == pricingTierId)
            ?? throw DomainErrors.PricingTier.NotFound(pricingTierId);

        if (RemainingCapacity < quantity)
        {
            throw DomainErrors.TicketPurchase.InsufficientCapacity(
                quantity,
                RemainingCapacity);
        }

        SoldTickets += quantity;
        Version++;

        return TicketPurchase.Create(
            Id,
            pricingTierId,
            idempotencyKey.Trim(),
            requestFingerprint,
            purchaserName.Trim(),
            purchaserEmail.Trim(),
            quantity,
            tier.Price,
            purchasedAtUtc);
    }
    public void EnsureCanBeDeleted()
    {
        if (SoldTickets > 0)
        {
            throw DomainErrors.Event.HasSoldTickets();
        }
    }

    #region Validation Methods
    private static void ValidateCapacity(
      int totalCapacity)
    {
        if (totalCapacity <= 0)
        {
            throw DomainErrors.Event.CapacityMustBePositive();
        }
    }

    private static void ValidatePricingTiers(
        IReadOnlyCollection<PricingTierDefinition> pricingTiers)
    {
        if (pricingTiers is null ||
            pricingTiers.Count == 0)
        {
            throw DomainErrors.PricingTier.AtLeastOneRequired();
        }

        foreach (var tier in pricingTiers)
        {
            ValidatePricingTier(
                tier.Name,
                tier.Price);
        }

        var duplicate = pricingTiers
            .GroupBy(
                x => x.Name.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(
                x => x.Count() > 1);

        if (duplicate is not null)
        {
            throw DomainErrors.PricingTier.DuplicateName(
                duplicate.Key);
        }
    }

    private static void ValidatePricingTier(
        string name,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw DomainErrors.PricingTier.NameRequired();
        }

        if (price <= 0)
        {
            throw DomainErrors.PricingTier.PriceMustBePositive();
        }
    }

    private static void ValidatePurchase(
        int quantity,
        string idempotencyKey,
        string requestFingerprint,
        string purchaserName,
        string purchaserEmail)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw DomainErrors.TicketPurchase.IdempotencyKeyRequired();
        }

        if (string.IsNullOrWhiteSpace(requestFingerprint))
        {
            throw DomainErrors.TicketPurchase.RequestFingerprintRequired();
        }

        if (quantity <= 0)
        {
            throw DomainErrors.TicketPurchase.QuantityMustBePositive();
        }

        if (quantity >
            DomainErrors.TicketPurchase.MaxQuantityPerPurchase)
        {
            throw DomainErrors.TicketPurchase
                .QuantityExceedsMaxPerPurchase();
        }

        if (string.IsNullOrWhiteSpace(purchaserName))
        {
            throw DomainErrors.TicketPurchase.PurchaserNameRequired();
        }

        if (!IsValidEmail(purchaserEmail))
        {
            throw DomainErrors.TicketPurchase.PurchaserEmailInvalid();
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ValidateDetails(
        string name,
        string description,
        string venue,
        DateOnly eventDate,
        TimeOnly eventTime)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw DomainErrors.Event.NameRequired();
        if (name.Trim().Length > 200)
            throw DomainErrors.Event.NameTooLong();
        if (string.IsNullOrWhiteSpace(description))
            throw DomainErrors.Event.DescriptionRequired();
        if (description.Trim().Length > 2000)
            throw DomainErrors.Event.DescriptionTooLong();
        if (string.IsNullOrWhiteSpace(venue))
            throw DomainErrors.Event.VenueRequired();
        if (string.IsNullOrWhiteSpace(eventDate.ToString()))
            throw DomainErrors.Event.DateRequired();
        if (string.IsNullOrWhiteSpace(eventTime.ToString()))
            throw DomainErrors.Event.TimeRequired();
    }
    private static void ValidateFutureEventDateTime(
        DateOnly eventDate,
        TimeOnly eventTime,
        DateTime nowUtc)
    {
        if (eventDate == default)
        {
            throw DomainErrors.Event.DateRequired();
        }

        var eventDateTimeUtc = DateTime.SpecifyKind(
            eventDate.ToDateTime(eventTime),
            DateTimeKind.Utc);

        if (eventDateTimeUtc <= nowUtc)
        {
            throw DomainErrors.Event.MustBeInTheFuture();
        }
    }

    #endregion
}
