using Application.Common;
using Application.Common.Abstraction.Handlers;
using Application.Common.Idempotent;
using Application.Features.Tickets.Purchase.Command;
using Application.Features.Tickets.Shared;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Application.Features.Tickets.Purchase.Handler;

public sealed class PurchaseTicketsCommandHandler(
    IApplicationDbContext db)
    : ICommandHandler<
        PurchaseTicketsCommand,
        IdempotentResult<TicketPurchaseResponse>>
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<IdempotentResult<TicketPurchaseResponse>> Handle(
        PurchaseTicketsCommand command,
        CancellationToken cancellationToken)
    {
        var fingerprint = CreateFingerprint(command);

        for (var attempt = 0;
             attempt < MaxConcurrencyRetries;
             attempt++)
        {
            var existingPurchase =
                await FindExistingPurchaseAsync(
                    command,
                    fingerprint,
                    cancellationToken);

            if (existingPurchase is not null)
            {
                return IdempotentResult<TicketPurchaseResponse>.Replay(
                    TicketPurchaseResponse.From(existingPurchase));
            }

            db.ClearTracking();

            var @event = await db.Events
                .Include(x => x.PricingTiers)
                .SingleOrDefaultAsync(
                    x => x.Id == command.EventId,
                    cancellationToken);

            if (@event is null)
            {
                throw DomainErrors.Event.NotFound(
                    command.EventId);
            }

            var purchase = @event.PurchaseTickets(
                command.PricingTierId,
                command.Quantity,
                command.IdempotencyKey,
                fingerprint,
                command.PurchaserName,
                command.PurchaserEmail,
                DateTime.UtcNow);

            db.TicketPurchases.Add(purchase);

            try
            {
                await db.SaveChangesAsync(
                    cancellationToken);

                return IdempotentResult<TicketPurchaseResponse>.Created(
                    TicketPurchaseResponse.From(purchase));
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ClearTracking();

                var replay =
                    await FindExistingPurchaseAsync(
                        command,
                        fingerprint,
                        cancellationToken);

                if (replay is not null)
                {
                    return IdempotentResult<TicketPurchaseResponse>.Replay(
                        TicketPurchaseResponse.From(replay));
                }

                // Another purchase changed inventory.
                // Retry by reloading the current Event.
            }
            catch (DbUpdateException)
            {
                db.ClearTracking();

                var replay =
                    await FindExistingPurchaseAsync(
                        command,
                        fingerprint,
                        cancellationToken);

                if (replay is not null)
                {
                    return IdempotentResult<TicketPurchaseResponse>.Replay(
                        TicketPurchaseResponse.From(replay));
                }

                throw;
            }
        }

        throw DomainErrors.TicketPurchase.InventoryChanged();
    }

    private async Task<TicketPurchase?> FindExistingPurchaseAsync(
        PurchaseTicketsCommand command,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var existing = await db.TicketPurchases
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EventId == command.EventId &&
                     x.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(
                existing.RequestFingerprint,
                fingerprint,
                StringComparison.Ordinal))
        {
            throw DomainErrors.TicketPurchase.IdempotencyKeyConflict();
        }

        return existing;
    }

    private static string CreateFingerprint(
        PurchaseTicketsCommand command)
    {
        var input = string.Join(
            "|",
            command.EventId,
            command.PricingTierId,
            command.Quantity,
            command.PurchaserName.Trim(),
            command.PurchaserEmail
                .Trim()
                .ToLowerInvariant());

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(input));

        return Convert.ToHexString(hash);
    }
}