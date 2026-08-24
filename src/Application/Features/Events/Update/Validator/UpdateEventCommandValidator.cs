using Application.Features.Events.Update.Command;
using FluentValidation;

namespace Application.Features.Events.Update.Validator;

public sealed class UpdatePricingTierRequestValidator : AbstractValidator<UpdatePricingTierRequest>
{
    public UpdatePricingTierRequestValidator()
    {
        RuleFor(x => x.Id)
       .NotEmpty()
       .WithMessage("Pricing tier ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Pricing tier name is required and must not exceed 100 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative.");
    }
}

public sealed record UpdatePricingTierRequest(
    Guid Id,
    string Name,
    decimal Price);

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EventDate)
            .Must(x => x > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date must be in the future.");
        RuleFor(x => x.EventTime).NotNull();
    }
}
