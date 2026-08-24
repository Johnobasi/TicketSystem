using Application.Features.Events.GetById.Query;
using FluentValidation;

namespace Application.Features.Events.GetById.Validator;

public sealed class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
{
    public GetEventByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Event ID is required.");
    }
}