namespace Application.Common.Abstraction.Handlers;

public interface ICommandHandler<in TCommand, TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}
