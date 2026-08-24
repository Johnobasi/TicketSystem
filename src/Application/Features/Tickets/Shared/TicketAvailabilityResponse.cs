namespace Application.Features.Tickets.Shared;

public sealed record TicketAvailabilityResponse(
    Guid EventId,
    int TotalCapacity,
    int SoldTickets,
    int RemainingCapacity,
    bool IsSoldOut);
