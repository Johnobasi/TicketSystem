using FluentValidation;

namespace Application.Features.Events.Update.Command;

public sealed class UpdateEventCommandValidator
    : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Event ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Event name is required and must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Event description is required and must not exceed 2000 characters.");

        RuleFor(x => x.Venue)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Event venue is required and must not exceed 200 characters.");

        RuleFor(x => x.EventDate)
            .NotEqual(default(DateOnly))
            .WithMessage("Event date is required.");

        RuleFor(x => x.EventTime)
            .NotEqual(default(TimeOnly))
            .WithMessage("Event time is required.");

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0)
            .WithMessage("Total capacity must be greater than zero.");
    }
}