using Application.Features.Reports.GetSalesSummary.Query;
using FluentValidation;

namespace Application.Features.Reports.GetSalesSummary.Validator;

public sealed class GetSalesByEventQueryValidator : AbstractValidator<GetSalesByEventQuery>
{
    public GetSalesByEventQueryValidator()
    {
        RuleFor(x => x.EventId).NotEqual(Guid.Empty);
    }
}
