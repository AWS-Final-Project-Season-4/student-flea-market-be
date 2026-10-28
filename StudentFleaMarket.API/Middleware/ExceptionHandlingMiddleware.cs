using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudentFleaMarket.Application.Common;

namespace StudentFleaMarket.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };
        if (status >= 500) logger.LogError(exception, "Unhandled request exception");
        else logger.LogWarning(exception, "Request failed with status {StatusCode}", status);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == 500 ? "An unexpected error occurred." : exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (exception is ValidationException validationException)
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
