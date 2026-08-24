using Application.Features.Tickets.Purchase.Command;
using Application.Features.Tickets.Purchase.Handler;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace TicketSystemTests.Integration;

public sealed class IdempotencyIntegrationTests(SqlServerFixture fixture)
    : SqlServerIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Fifty_concurrent_identical_requests_create_exactly_one_database_purchase()
    {
        var (eventId, pricingTierId) = await SeedEventAsync();
        const int requestCount = 50;
        const string idempotencyKey = "same-key-concurrent";

        var start = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => ExecuteIdenticalPurchaseAsync(
                eventId,
                pricingTierId,
                idempotencyKey,
                start.Task))
            .ToArray();

        start.SetResult(null);

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSuccess));

        await using var verificationDb = Fixture.CreateDbContext();
        Assert.Equal(1, await verificationDb.TicketPurchases.CountAsync());
        Assert.Equal(2, (await verificationDb.Events.SingleAsync(x => x.Id == eventId)).SoldTickets);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_payload_is_rejected_with_conflict()
    {
        var (eventId, pricingTierId) = await SeedEventAsync();
        const string idempotencyKey = "same-key-different-payload";

        await ExecutePurchaseAsync(
            eventId,
            pricingTierId,
            idempotencyKey,
            "Alice",
            "alice@example.com",
            2);

        await using var db = Fixture.CreateDbContext();
        var handler = new PurchaseTicketsCommandHandler(
            db);

        var command = new PurchaseTicketsCommand(
            eventId,
            pricingTierId,
            3, // Different quantity than the first purchase
            idempotencyKey,
            "Alice",
            "alice@example.com");

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("TicketPurchase.IdempotencyKeyConflict", exception.Code);
        Assert.Equal(1, await db.TicketPurchases.CountAsync());
    }

    private async Task<PurchaseAttemptResult> ExecuteIdenticalPurchaseAsync(
        Guid eventId,
        Guid pricingTierId,
        string idempotencyKey,
        Task start)
    {
        await start;
        return await ExecutePurchaseAsync(
            eventId,
            pricingTierId,
            idempotencyKey,
            "Alice",
            "alice@example.com",
            2);
    }

    private async Task<PurchaseAttemptResult> ExecutePurchaseAsync(
        Guid eventId,
        Guid pricingTierId,
        string idempotencyKey,
        string purchaserName,
        string purchaserEmail,
        int quantity)
    {
        await using var db = Fixture.CreateDbContext();
        var handler = new PurchaseTicketsCommandHandler(
            db);

        var command = new PurchaseTicketsCommand(
            eventId,
            pricingTierId,
            quantity,
            idempotencyKey,
            purchaserName,
            purchaserEmail
            );

        try
        {
            var response = await handler.Handle(command, CancellationToken.None);
            return PurchaseAttemptResult.Success(response.Value.Id);
        }
        catch (ConflictException exception)
        {
            throw new Xunit.Sdk.XunitException(
                $"An idempotent replay should succeed, but received {exception.Code}.");
        }
    }

    private async Task<(Guid EventId, Guid PricingTierId)> SeedEventAsync()
    {
        await using var db = Fixture.CreateDbContext();
        var @event = CreateEvent(capacity: 100);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var tier = @event.PricingTiers.Single(x => x.Name == "General");
        return (@event.Id, tier.Id);
    }

    private readonly record struct PurchaseAttemptResult(bool IsSuccess, Guid PurchaseId)
    {
        public static PurchaseAttemptResult Success(Guid purchaseId) => new(true, purchaseId);
    }
}
