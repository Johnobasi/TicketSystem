using Application.Common;
using Application.Features.Events.Retrieve.Query;
using FluentValidation;

public sealed class GetEventsQueryValidator
    : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, Pagination.MaxPageSize);
    }
}