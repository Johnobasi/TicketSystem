namespace Domain.Exceptions;

public static class DomainErrors
{
    public static class Event
    {
        public static DomainValidationException NameRequired() =>
            new("Event.NameRequired", "Event name is required.");

        public static DomainValidationException NameTooLong() =>
            new("Event.NameTooLong", "Event name cannot exceed 200 characters.");

        public static DomainValidationException DescriptionRequired() =>
            new("Event.DescriptionRequired", "Event description is required.");

        public static DomainValidationException DescriptionTooLong() =>
            new("Event.DescriptionTooLong", "Event description cannot exceed 2000 characters.");

        public static DomainValidationException VenueRequired() =>
            new("Event.VenueRequired", "Event venue is required.");

        public static DomainValidationException VenueTooLong() =>
            new("Event.VenueTooLong", "Event venue cannot exceed 200 characters.");

        public static DomainValidationException CapacityMustBePositive() =>
            new("Event.CapacityMustBePositive", "Total capacity must be greater than zero.");

        public static DomainValidationException CapacityBelowSoldTickets(int soldTickets) =>
            new("Event.CapacityBelowSoldTickets", $"Total capacity cannot be reduced below the {soldTickets} ticket(s) already sold.");

        public static DomainValidationException DateRequired() =>
            new("Event.DateRequired", "Event start date is required.");

        public static DomainValidationException TimeRequired() =>
            new("Event.TimeRequired", "Event start time is required.");

        public static DomainValidationException MustBeInTheFuture() =>
            new("Event.DateMustBeInFuture", "Event start date/time must be in the future.");


        public static ConflictException HasSoldTickets() =>
            new("Event.HasSoldTickets", "The event cannot be deleted because tickets have already been sold.");

        public static NotFoundException NotFound(Guid eventId) =>
            new("Event.NotFound", $"No event was found with id '{eventId}'.");
    }

    public static class PricingTier
    {
        public static DomainValidationException AtLeastOneRequired() =>
            new("PricingTier.AtLeastOneRequired", "At least one pricing tier is required.");

        public static DomainValidationException NameRequired() =>
            new("PricingTier.NameRequired", "Pricing tier name is required.");

        public static DomainValidationException NameTooLong() =>
            new("PricingTier.NameTooLong", "Pricing tier name cannot exceed 100 characters.");

        public static DomainValidationException PriceMustBePositive() =>
            new("PricingTier.PriceMustBePositive", "Pricing tier price must be greater than zero.");

        public static DomainValidationException DuplicateName(string name) =>
            new("PricingTier.DuplicateName", $"A pricing tier named '{name}' already exists for this event.");

        public static NotFoundException NotFound(Guid pricingTierId) =>
            new("PricingTier.NotFound", $"No pricing tier was found with id '{pricingTierId}'.");
    }

    public static class TicketPurchase
    {
        public const int MaxQuantityPerPurchase = 50;

        public static DomainValidationException QuantityMustBePositive() =>
            new("TicketPurchase.QuantityMustBePositive", "Quantity must be greater than zero.");

        public static DomainValidationException QuantityExceedsMaxPerPurchase() =>
            new("TicketPurchase.QuantityExceedsMaxPerPurchase", $"Quantity cannot exceed {MaxQuantityPerPurchase} per purchase.");

        public static DomainValidationException IdempotencyKeyRequired() =>
            new("TicketPurchase.IdempotencyKeyRequired", "An Idempotency-Key header is required.");

        public static DomainValidationException RequestFingerprintRequired() =>
            new("TicketPurchase.RequestFingerprintRequired", "A request fingerprint is required.");

        public static DomainValidationException PurchaserNameRequired() =>
            new("TicketPurchase.PurchaserNameRequired", "Purchaser name is required.");

        public static DomainValidationException PurchaserEmailInvalid() =>
            new("TicketPurchase.PurchaserEmailInvalid", "A valid purchaser email is required.");

        public static ConflictException InsufficientCapacity(int requested, int remaining) =>
            new("TicketPurchase.InsufficientCapacity", $"Only {remaining} ticket(s) remain; requested {requested}.");

        public static DomainValidationException IdempotencyKeyReuse() =>
            new(
                "TicketPurchase.IdempotencyKeyReuse",
                "The idempotency key has already been used with a different request.");

        public static ConflictException IdempotencyKeyConflict() =>
            new(
                "TicketPurchase.IdempotencyKeyConflict",
                "The idempotency key is in conflict with an existing purchase.");

        public static DomainValidationException InventoryChanged() =>
            new(
                "TicketPurchase.InventoryChanged",
                "Ticket inventory changed while the purchase was being processed. Please retry.");
    }
}
