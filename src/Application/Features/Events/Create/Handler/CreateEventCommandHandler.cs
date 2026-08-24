using Application.Common;
using Application.Features.Events.Create.Command;
using Domain.Entities;
using Domain.Models;
using Application.Common.Abstraction.Handlers;

namespace Application.Features.Events.Create.Handler;

public sealed class CreateEventCommandHandler(
    IApplicationDbContext db)
    : ICommandHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateEventCommand command,
        CancellationToken cancellationToken)
    {
        var pricingTiers = command.PricingTiers
            .Select(x => new PricingTierDefinition(x.Name, x.Price))
            .ToList();

        var @event = Event.Create(
            command.Name,
            command.Description,
            command.Venue,
            command.EventDate,
            command.EventTime,
            command.TotalCapacity,
            pricingTiers,
            DateTime.UtcNow);

        db.Events.Add(@event);
        await db.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
