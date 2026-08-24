using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicketSystemTests.Integration;

public sealed class ApiIntegrationTests(SqlServerFixture fixture)
    : SqlServerIntegrationTestBase(fixture)
{
    private CustomWebApplicationFactory? _factory;
    private HttpClient? _client;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _factory = new CustomWebApplicationFactory(
            Fixture.ConnectionString);

        _client = _factory.CreateClient();
    }

    public override Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();

        return Task.CompletedTask;
    }

    private HttpClient Client =>
        _client
        ?? throw new InvalidOperationException(
            "Test client has not been initialized.");

    // ============================================================
    // CREATE EVENT
    // ============================================================

    [Fact]
    public async Task Create_event_returns_201_and_location_header()
    {
        var request = new
        {
            name = "API Test Event",
            description = "An API integration test event.",
            venue = "London",
            eventDate = "2030-01-01",
            eventTime = "19:00:00",
            totalCapacity = 100,
            pricingTiers = new[]
            {
                new
                {
                    name = "General",
                    price = 25.00m
                },
                new
                {
                    name = "VIP",
                    price = 100.00m
                }
            }
        };

        var response = await Client.PostAsJsonAsync(
            "/api/events",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(
            response.Headers.Location);
    }

    // ============================================================
    // IDEMPOTENCY
    // ============================================================

    [Fact]
    public async Task Purchase_replay_with_same_idempotency_key_returns_same_purchase()
    {
        var eventId = await SeedEventAsync();

        var pricingTierId =
            await GetGeneralTierIdAsync(eventId);

        var request = new
        {
            pricingTierId,
            purchaserName = "Alice",
            purchaserEmail = "alice@example.com",
            quantity = 1
        };

        using var firstRequest = CreatePurchaseRequest(
            eventId,
            request,
            "api-replay-key");

        using var secondRequest = CreatePurchaseRequest(
            eventId,
            request,
            "api-replay-key");

        var firstResponse =
            await Client.SendAsync(firstRequest);

        var secondResponse =
            await Client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode);

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<ApiPurchaseResponse>();

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<ApiPurchaseResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);

        Assert.Equal(
            first!.Id,
            second!.Id);

        var @event =
            await GetEventAsync(eventId);

        Assert.Equal(
            1,
            @event.SoldTickets);
    }

    // ============================================================
    // CONCURRENCY / OVERSOLD INVENTORY
    // ============================================================

    [Fact]
    public async Task Concurrent_purchases_do_not_oversell_inventory()
    {
        var eventId = await SeedEventAsync(
            capacity: 1);

        var pricingTierId =
            await GetGeneralTierIdAsync(eventId);

        using var requestA = CreatePurchaseRequest(
            eventId,
            new
            {
                pricingTierId,
                purchaserName = "Alice",
                purchaserEmail = "alice@example.com",
                quantity = 1
            },
            "concurrency-key-a");

        using var requestB = CreatePurchaseRequest(
            eventId,
            new
            {
                pricingTierId,
                purchaserName = "Bob",
                purchaserEmail = "bob@example.com",
                quantity = 1
            },
            "concurrency-key-b");

        var responses = await Task.WhenAll(
            Client.SendAsync(requestA),
            Client.SendAsync(requestB));

        var successfulPurchases = responses.Count(
            x => x.StatusCode == HttpStatusCode.Created);

        Assert.Equal(
            1,
            successfulPurchases);

        var @event =
            await GetEventAsync(eventId);

        Assert.Equal(
            1,
            @event.SoldTickets);

        Assert.Equal(
            0,
            @event.RemainingCapacity);

        Assert.True(
            @event.IsSoldOut);
    }

    // ============================================================
    // TEST HELPERS
    // ============================================================

    private static HttpRequestMessage CreatePurchaseRequest<T>(
        Guid eventId,
        T request,
        string idempotencyKey)
    {
        var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/events/{eventId}/purchases")
        {
            Content = JsonContent.Create(request)
        };

        message.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        return message;
    }

    private async Task<Guid> SeedEventAsync(
        int capacity = 100)
    {
        await using var db =
            Fixture.CreateDbContext();

        var @event = Domain.Entities.Event.Create(
            name: "API Purchase Test Event",
            description: "An API integration test event.",
            venue: "London",
            eventDate: new DateOnly(2030, 1, 1),
            eventTime: new TimeOnly(19, 0),
            totalCapacity: capacity,
            pricingTiers:
            [
                new Domain.Models.PricingTierDefinition(
                    "General",
                    25.00m)
            ],
            nowUtc: DateTime.UtcNow);

        db.Events.Add(@event);

        await db.SaveChangesAsync();

        return @event.Id;
    }

    private async Task<Guid> GetGeneralTierIdAsync(
        Guid eventId)
    {
        var @event =
            await GetEventAsync(eventId);

        return @event
            .PricingTiers
            .Single(x => x.Name == "General")
            .Id;
    }

    private async Task<ApiEventResponse> GetEventAsync(
        Guid eventId)
    {
        var response = await Client.GetAsync(
            $"/api/events/{eventId}");

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"GET /api/events/{eventId} failed.\n" +
            $"Status: {(int)response.StatusCode} " +
            $"{response.StatusCode}\n" +
            $"Response: {body}");

        var result =
            await response.Content
                .ReadFromJsonAsync<ApiEventResponse>();

        Assert.NotNull(result);

        return result!;
    }

    // ============================================================
    // API CONTRACTS
    // ============================================================

    private sealed record ApiEventResponse(
        Guid Id,
        string Name,
        string Description,
        string Venue,
        DateOnly EventDate,
        TimeOnly EventTime,
        int TotalCapacity,
        int SoldTickets,
        int RemainingCapacity,
        bool IsSoldOut,
        IReadOnlyCollection<ApiPricingTierResponse>
            PricingTiers);

    private sealed record ApiPricingTierResponse(
        Guid Id,
        string Name,
        decimal Price);

    private sealed record ApiPurchaseResponse(
        Guid Id);
}

// ================================================================
// WEB APPLICATION FACTORY
// ================================================================

internal sealed class CustomWebApplicationFactory(
    string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            connectionString);
    }
}