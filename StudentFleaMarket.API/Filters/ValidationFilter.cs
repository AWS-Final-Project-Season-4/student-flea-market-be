using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StudentFleaMarket.API.Filters;

/// <summary>
/// Tự động chạy FluentValidation cho mọi request DTO có validator tương ứng.
/// Validator được tra cứu theo đúng kiểu tham số của action.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(ILogger<ValidationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            // Bỏ qua các tham số không phải DTO (vd. CancellationToken)
            if (argument is CancellationToken) continue;

            var validator = ResolveValidator(context.HttpContext.RequestServices, argument.GetType());
            if (validator is null) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                    .ToList();

                _logger.LogWarning("Validation failed for {Type}: {Errors}",
                    argument.GetType().Name, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));

                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Invalid Data.",
                    code = "VALIDATION_ERROR",
                    errors
                });
                return;
            }
        }

        await next();
    }

    private static IValidator? ResolveValidator(IServiceProvider services, Type argumentType)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
        return services.GetService(validatorType) as IValidator;
    }
}
