using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rehably.API.Filters;

public class ValidationActionFilter : IAsyncActionFilter
{
    private static bool IsSimpleType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string)
            || underlying == typeof(decimal) || underlying == typeof(Guid)
            || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (key, value) in context.ActionArguments)
        {
            if (value == null) continue;

            // Skip primitive/simple types — only validate complex request DTOs
            if (IsSimpleType(value.GetType())) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(value);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    error = new { code = "VALIDATION_ERROR", message = "Invalid input", details = errors }
                });
                return;
            }
        }
        await next();
    }
}
