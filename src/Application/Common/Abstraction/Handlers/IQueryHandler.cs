namespace Application.Common.Abstraction.Handlers;

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}
