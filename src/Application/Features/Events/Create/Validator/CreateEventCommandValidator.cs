using Application.Features.Events.Create.Command;
using FluentValidation;

namespace Application.Features.Events.Create.Validator;

public sealed class CreatePricingTierRequestValidator : AbstractValidator<CreatePricingTierRequest>
{
    public CreatePricingTierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0);
    }
}

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EventDate)
            .Must(x => x >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date must be in the future.");
        RuleFor(x => x.EventTime).NotEmpty().WithMessage("Event time is required.");
        RuleFor(x => x.TotalCapacity).GreaterThan(0);
        RuleFor(x => x.PricingTiers)
            .NotEmpty()
            .WithMessage("At least one pricing tier is required.");
        RuleForEach(x => x.PricingTiers)
            .SetValidator(new CreatePricingTierRequestValidator());
        RuleFor(x => x.PricingTiers)
            .Must(tiers => tiers
                .Select(t => t.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == tiers.Count)
            .WithMessage("Pricing tier names must be unique within an event.")
            .When(x => x.PricingTiers is { Count: > 0 });
    }
}
