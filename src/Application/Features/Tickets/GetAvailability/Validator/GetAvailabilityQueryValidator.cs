using Application.Features.Tickets.GetAvailability.Query;
using FluentValidation;

namespace Application.Features.Tickets.GetAvailability.Validator;

public sealed class GetAvailabilityQueryValidator : AbstractValidator<GetTicketAvailabilityQuery>
{
    public GetAvailabilityQueryValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("EventId is required.");
    }
}
