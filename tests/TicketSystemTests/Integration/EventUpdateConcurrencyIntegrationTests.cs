using Microsoft.EntityFrameworkCore;

namespace TicketSystemTests.Integration;

public sealed class EventUpdateConcurrencyIntegrationTests(SqlServerFixture fixture)
    : SqlServerIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Concurrent_event_updates_allow_one_writer_and_reject_the_stale_writer()
    {
        Guid eventId;

        await using (var setupDb = Fixture.CreateDbContext())
        {
            var @event = CreateEvent();
            setupDb.Events.Add(@event);
            await setupDb.SaveChangesAsync();
            eventId = @event.Id;
        }

        await using var firstDb = Fixture.CreateDbContext();
        await using var secondDb = Fixture.CreateDbContext();

        var first = await firstDb.Events
            .Include(x => x.PricingTiers)
            .SingleAsync(x => x.Id == eventId);

        var second = await secondDb.Events
            .Include(x => x.PricingTiers)
            .SingleAsync(x => x.Id == eventId);

        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstVersion = first.Version;
        var secondVersion = second.Version;

        first.UpdateDetails(
            "First update",
            first.Description,
            first.Venue,
            first.EventDate,
            first.EventTime,
            first.TotalCapacity,
            now.AddDays(1));

        second.UpdateDetails(
            "Second update",
            second.Description,
            second.Venue,
            second.EventDate,
            second.EventTime,
            second.TotalCapacity,
            now.AddDays(1));

        Assert.Equal(firstVersion + 1, first.Version);
        Assert.Equal(secondVersion + 1, second.Version);

        await firstDb.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondDb.SaveChangesAsync());

        Assert.NotNull(exception);

        await using var verificationDb = Fixture.CreateDbContext();
        var persistedEvent = await verificationDb.Events.SingleAsync(x => x.Id == eventId);

        Assert.Equal("First update", persistedEvent.Name);
        Assert.Equal(1, persistedEvent.Version);
    }
}
