using Application.Common;
using Application.Common.Abstraction.Handlers;
using Application.Features.Events.Delete.Command;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Events.Delete.Handler;

public sealed class DeleteEventCommandHandler(IApplicationDbContext db)
    : ICommandHandler<DeleteEventCommand>
{
    public async Task Handle(
        DeleteEventCommand command,
        CancellationToken cancellationToken)
    {
        var @event = await db.Events
            .SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (@event is null)
        {
            throw DomainErrors.Event.NotFound(command.Id);
        }

        @event.EnsureCanBeDeleted();
        db.Events.Remove(@event);

        await db.SaveChangesAsync(cancellationToken);
    }
}
