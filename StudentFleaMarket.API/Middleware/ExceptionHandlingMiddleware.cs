using System.Text.Json;
using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.Common.Exceptions;

namespace StudentFleaMarket.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

       public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "AppException: {Code} - {Message}", ex.Code, ex.Message);
            await WriteResponseAsync(context, MapStatusCode(ex), ApiResponse.Fail(ex.Message, ex.Code));
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status401Unauthorized,
                ApiResponse.Fail(ex.Message, "UNAUTHORIZED"));
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogWarning(ex, "BadHttpRequestException: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest,
                ApiResponse.Fail(ex.Message, "BAD_REQUEST"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "ArgumentException: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest,
                ApiResponse.Fail(ex.Message, "INVALID_ARGUMENT"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "KeyNotFoundException: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status404NotFound,
                ApiResponse.Fail(ex.Message, "NOT_FOUND"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was cancelled by client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError,
                ApiResponse.Fail("A system error has occurred. Please try again later.", "INTERNAL_ERROR"));
        }
    }


    private static int MapStatusCode(AppException ex) => ex switch
    {
        ValidationException => StatusCodes.Status400BadRequest,
        UnauthorizedException => StatusCodes.Status401Unauthorized,
        ForbiddenException => StatusCodes.Status403Forbidden,
        NotFoundException => StatusCodes.Status404NotFound,
        ConflictException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, ApiResponse body)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
