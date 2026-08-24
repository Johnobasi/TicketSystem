using Application.Features.Events.Create.Command;
using Application.Features.Events.Create.Validator;
using Application.Features.Events.Retrieve.Query;
using Application.Features.Reports.GetSalesSummary.Query;
using Application.Features.Reports.GetSalesSummary.Validator;
using Application.Features.Tickets.GetAvailability.Query;
using Application.Features.Tickets.GetAvailability.Validator;
using Application.Features.Tickets.Purchase.Command;
using Application.Features.Tickets.Purchase.Validator;

namespace TicketSystemTests;

public sealed class ValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_validator_rejects_non_positive_price(
        decimal price)
    {
        var validator = new CreateEventCommandValidator();

        var command = new CreateEventCommand(
            "Event",
            "Description",
            "Venue",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new TimeOnly(19, 0),
            100,
            [
                new CreatePricingTierRequest(
                    "General",
                    price)
            ]);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName.Contains("Price"));
    }

    [Theory]
    [InlineData("General", "general")]
    [InlineData("VIP", "vip")]
    [InlineData("Standard", "STANDARD")]
    public void Create_validator_rejects_duplicate_tier_names(
        string firstName,
        string secondName)
    {
        var validator = new CreateEventCommandValidator();

        var command = new CreateEventCommand(
            "Event",
            "Description",
            "Venue",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new TimeOnly(19, 0),
            100,
            [
                new CreatePricingTierRequest(firstName, 20),
                new CreatePricingTierRequest(secondName, 30)
            ]);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName.Contains("PricingTiers"));
    }

    [Theory]
    [InlineData(51)]
    [InlineData(100)]
    public void Purchase_validator_rejects_quantity_above_limit(
        int quantity)
    {
        var validator = new PurchaseTicketCommandValidator();

        var command = new PurchaseTicketsCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
              quantity,
            "key",
            "Alice",
            "alice@example.com"
            );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == nameof(PurchaseTicketsCommand.Quantity));
    }

    [Fact]
    public void Availability_validator_requires_event_id()
    {
        var validator = new GetAvailabilityQueryValidator();

        var result = validator.Validate(
            new GetTicketAvailabilityQuery(Guid.Empty));

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == "EventId");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 101)]
    [InlineData(0, 101)]
    public void Events_pagination_validator_rejects_invalid_pagination(
        int page,
        int pageSize)
    {
        var validator = new GetEventsQueryValidator();

        var result = validator.Validate(
            new GetEventsQuery(page, pageSize));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Report_validator_rejects_empty_event_id()
    {
        var validator = new GetSalesByEventQueryValidator();

        var result = validator.Validate(
            new GetSalesByEventQuery(Guid.Empty));

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.PropertyName == "EventId");
    }
}