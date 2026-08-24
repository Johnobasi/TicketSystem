using FluentValidation;
using Application.Features.Tickets.Purchase.Command;

namespace Application.Features.Tickets.Purchase.Validator;

public sealed class PurchaseTicketCommandValidator
    : AbstractValidator<PurchaseTicketsCommand>
{
    private const int MaxQuantityPerPurchase = 50;

    public PurchaseTicketCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");

        RuleFor(x => x.PricingTierId)
            .NotEmpty()
            .WithMessage("Pricing tier ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxQuantityPerPurchase)
            .WithMessage(
                $"Quantity must be between 1 and {MaxQuantityPerPurchase}.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required.");

        RuleFor(x => x.PurchaserName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Purchaser name is required.");

        RuleFor(x => x.PurchaserEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid purchaser email is required.");
    }
}