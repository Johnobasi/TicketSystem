using Application.Common;
using Application.Common.Abstraction.Handlers;
using Application.Features.Events.Update.Command;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Events.Update.Handler;

public sealed class UpdateEventCommandHandler(
    IApplicationDbContext db)
    : ICommandHandler<UpdateEventCommand>
{
    public async Task Handle(
        UpdateEventCommand command,
        CancellationToken cancellationToken)
    {
        var @event = await db.Events
            .Include(x => x.PricingTiers)
            .SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if(@event is null)
        {
            throw DomainErrors.Event.NotFound(command.Id);
        }


        @event.UpdateDetails(
            command.Name,
            command.Description,
            command.Venue,
            command.EventDate,
            command.EventTime,
            command.TotalCapacity,
            DateTime.UtcNow);

        await db.SaveChangesAsync(cancellationToken);
    }
}
