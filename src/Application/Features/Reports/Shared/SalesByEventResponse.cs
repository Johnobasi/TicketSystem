namespace Application.Features.Reports.Shared;

public sealed record SalesByEventResponse(
  Guid EventId,
    int TicketsSold,
    decimal Revenue);
