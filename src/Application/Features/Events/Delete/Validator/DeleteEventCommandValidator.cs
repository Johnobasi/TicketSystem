using FluentValidation;

namespace Application.Features.Events.Delete.Command;

public sealed class DeleteEventCommandValidator
    : AbstractValidator<DeleteEventCommand>
{
    public DeleteEventCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Event ID is required.");
    }
}