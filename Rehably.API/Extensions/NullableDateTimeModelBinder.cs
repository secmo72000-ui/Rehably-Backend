using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Rehably.API.Extensions;

/// <summary>
/// Treats empty or whitespace form values as null for DateTime? fields.
/// Swagger sends the current timestamp as a default value for date inputs —
/// this binder returns null when the field is blank so optional dates are skipped.
/// </summary>
public class NullableDateTimeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"'{value}' is not a valid date.");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
