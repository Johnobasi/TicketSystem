namespace Application.Features.Events.Retrieve.Query;

public sealed record GetEventsQuery(
    int Page = 1,
    int PageSize = 20);
