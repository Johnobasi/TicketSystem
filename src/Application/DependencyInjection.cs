using Application.Common;
using Application.Common.Abstraction;
using Application.Common.Abstraction.Handlers;
using Application.Common.Idempotent;
using Application.Features.Events.Create.Command;
using Application.Features.Events.Create.Handler;
using Application.Features.Events.Delete.Command;
using Application.Features.Events.Delete.Handler;
using Application.Features.Events.GetById.Handler;
using Application.Features.Events.GetById.Query;
using Application.Features.Events.Retrieve.Handler;
using Application.Features.Events.Retrieve.Query;
using Application.Features.Events.Shared;
using Application.Features.Events.Update.Command;
using Application.Features.Events.Update.Handler;
using Application.Features.Reports.GetSalesSummary.Handler;
using Application.Features.Reports.GetSalesSummary.Query;
using Application.Features.Reports.Shared;
using Application.Features.Tickets.GetAvailability.Handler;
using Application.Features.Tickets.GetAvailability.Query;
using Application.Features.Tickets.Purchase;
using Application.Features.Tickets.Purchase.Command;
using Application.Features.Tickets.Purchase.Handler;
using Application.Features.Tickets.Shared;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateEventCommand, Guid>, CreateEventCommandHandler>();
        services.AddScoped<IQueryHandler<GetEventsQuery, PagedResult<EventResponse>>, GetEventsQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateEventCommand>, UpdateEventCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteEventCommand>, DeleteEventCommandHandler>();
        services.AddScoped<ICommandHandler<PurchaseTicketsCommand,  IdempotentResult<TicketPurchaseResponse>>, PurchaseTicketsCommandHandler>();
        services.AddScoped<IQueryHandler<GetTicketAvailabilityQuery, TicketAvailabilityResponse>, GetTicketAvailabilityQueryHandler>();
        services.AddScoped<IQueryHandler<GetSalesByEventQuery, PagedResult<SalesByEventResponse>>, GetSalesByEventQueryHandler>();
        services.AddScoped<IQueryHandler<GetEventByIdQuery, EventResponse>, GetEventByIdQueryHandler>();
        services.AddScoped<IMediator, Mediator>();

        services.AddValidatorsFromAssembly(
            typeof(CreateEventCommand).Assembly);

        return services;
    }
}
