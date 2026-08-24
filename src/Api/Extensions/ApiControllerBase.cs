using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Api.Extensions;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult ValidationFailure(ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.ErrorMessage).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        });
    }
}
