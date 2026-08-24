using Application.Features.Tickets.Purchase.Command;
using Application.Features.Tickets.Purchase.Handler;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace TicketSystemTests.Integration;

public sealed class PurchaseConcurrencyIntegrationTests(SqlServerFixture fixture)
    : SqlServerIntegrationTestBase(fixture)
{
    [Fact]
    public async Task One_ticket_and_100_concurrent_purchases_result_in_exactly_one_success()
    {
        await using (var setupDb = Fixture.CreateDbContext())
        {
            var @event = CreateEvent(capacity: 1);
            setupDb.Events.Add(@event);
            await setupDb.SaveChangesAsync();
        }

        Guid eventId;
        Guid pricingTierId;
        await using (var readDb = Fixture.CreateDbContext())
        {
            var @event = await readDb.Events
                .Include(x => x.PricingTiers)
                .SingleAsync();

            eventId = @event.Id;
            pricingTierId = @event.PricingTiers.Single(x => x.Name == "General").Id;
        }

        const int requestCount = 100;
        var start = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, requestCount)
            .Select(index => ExecutePurchaseAsync(
                eventId,
                pricingTierId,
                index,
                start.Task))
            .ToArray();

        start.SetResult(null);

        var results = await Task.WhenAll(tasks);
        var successful = results.Count(x => x.IsSuccess);
        var failures = results.Count(x => !x.IsSuccess);

        Assert.Equal(1, successful);
        Assert.Equal(99, failures);

        await using var verificationDb = Fixture.CreateDbContext();
        var persistedEvent = await verificationDb.Events.SingleAsync(x => x.Id == eventId);
        var purchaseCount = await verificationDb.TicketPurchases.CountAsync();

        Assert.Equal(1, persistedEvent.SoldTickets);
        Assert.Equal(1, purchaseCount);
    }

    private async Task<PurchaseAttemptResult> ExecutePurchaseAsync(
        Guid eventId,
        Guid pricingTierId,
        int index,
        Task start)
    {
        await start;

        await using var db = Fixture.CreateDbContext();
        var handler = new PurchaseTicketsCommandHandler(
            db);

        var command = new PurchaseTicketsCommand(
            eventId,
            pricingTierId,
            1,
            $"concurrent-key-{index}",
            $"Buyer {index}",
            $"buyer{index}@example.com");

        try
        {
            await handler.Handle(command, CancellationToken.None);
            return PurchaseAttemptResult.Success();
        }
        catch (ConflictException)
        {
            return PurchaseAttemptResult.Failure();
        }
    }

    private readonly record struct PurchaseAttemptResult(bool IsSuccess)
    {
        public static PurchaseAttemptResult Success() => new(true);
        public static PurchaseAttemptResult Failure() => new(false);
    }
}
