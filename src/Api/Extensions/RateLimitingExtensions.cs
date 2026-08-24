using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Rate limit exceeded",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Too many ticket purchase requests. Please try again later.",
                    instance = context.HttpContext.Request.Path,
                    traceId = context.HttpContext.TraceIdentifier
                }, cancellationToken);
            };

            options.AddPolicy(
                RateLimiterPolicies.TicketPurchase,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });

        return services;
    }
}

public static class RateLimiterPolicies
{
    public const string TicketPurchase = "TicketPurchase";
}
