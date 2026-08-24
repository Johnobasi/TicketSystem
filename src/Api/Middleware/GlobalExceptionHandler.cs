using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        if (exception is ValidationException validationException)
        {
            await WriteValidationProblemAsync(
                httpContext,
                validationException,
                cancellationToken);

            return true;
        }

        var (status, title, detail) = exception switch
        {
            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                ex.Code,
                ex.Message),

            DomainValidationException ex => (
                StatusCodes.Status400BadRequest,
                ex.Code,
                ex.Message),

            ConflictException ex => (
                StatusCodes.Status409Conflict,
                ex.Code,
                ex.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Server.Error",
                "An unexpected error occurred.")
        };

        LogException(
            httpContext,
            exception,
            status);

        await WriteProblemAsync(
            httpContext,
            status,
            title,
            detail,
            cancellationToken);

        return true;
    }

    private async Task WriteValidationProblemAsync(
        HttpContext httpContext,
        ValidationException exception,
        CancellationToken cancellationToken)
    {
        var errors = exception.Errors
            .GroupBy(
                error => error.PropertyName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .Distinct()
                    .ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path,
            Type = "https://httpstatuses.com/400"
        };

        AddTraceId(problem, httpContext);

        httpContext.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);
    }

    private static async Task WriteProblemAsync(
        HttpContext httpContext,
        int status,
        string title,
        string detail,
        CancellationToken cancellationToken)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };

        AddTraceId(problem, httpContext);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);
    }

    private void LogException(
        HttpContext httpContext,
        Exception exception,
        int status)
    {
        if (exception is DomainException domainException)
        {
            logger.LogWarning(
                "Business error {Code} returned {StatusCode} for {Method} {Path}. TraceId: {TraceId}",
                domainException.Code,
                status,
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);

            return;
        }

        logger.LogError(
            exception,
            "Unhandled exception returned {StatusCode} for {Method} {Path}. TraceId: {TraceId}",
            status,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);
    }

    private static void AddTraceId(
        ProblemDetails problem,
        HttpContext httpContext)
    {
        problem.Extensions["traceId"] =
            httpContext.TraceIdentifier;
    }
}