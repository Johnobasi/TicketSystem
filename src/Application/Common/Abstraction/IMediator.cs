namespace Application.Common.Abstraction
{
    public interface IMediator
    {
        Task<TResult> Send<TResult>(
            object request,
            CancellationToken cancellationToken);

        Task Send(
            object request,
            CancellationToken cancellationToken);
    }
}
