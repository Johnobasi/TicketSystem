using Application.Common.Abstraction.Handlers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common.Abstraction;

public sealed class Mediator(
    IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResult> Send<TResult>(
        object request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);

        var requestType = request.GetType();

        // Commands with a response
        var commandHandlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(
                requestType,
                typeof(TResult));

        var commandHandler =
            serviceProvider.GetService(commandHandlerType);

        if (commandHandler is not null)
        {
            return await ((dynamic)commandHandler).Handle(
                (dynamic)request,
                cancellationToken);
        }

        // Queries
        var queryHandlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(
                requestType,
                typeof(TResult));

        var queryHandler =
            serviceProvider.GetRequiredService(queryHandlerType);

        return await ((dynamic)queryHandler).Handle(
            (dynamic)request,
            cancellationToken);
    }

    public async Task Send(
        object request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);

        var requestType = request.GetType();

        var handlerType = typeof(ICommandHandler<>)
            .MakeGenericType(requestType);

        var handler =
            serviceProvider.GetRequiredService(handlerType);

        await ((dynamic)handler).Handle(
            (dynamic)request,
            cancellationToken);
    }

    private async Task ValidateAsync(
        object request,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        var validatorType = typeof(IValidator<>)
            .MakeGenericType(requestType);

        var validators = serviceProvider
            .GetServices(validatorType)
            .Cast<IValidator>()
            .ToList();

        if (validators.Count == 0)
        {
            return;
        }

        var context = new ValidationContext<object>(request);

        var failures = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(
                context,
                cancellationToken);

            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}