namespace Application.Features.Events.Update.Command
{
    public sealed record UpdateEventCommand(
     Guid Id,
     string Name,
     string Description,
     string Venue,
     DateOnly EventDate,
     TimeOnly EventTime,
     int TotalCapacity);
}
