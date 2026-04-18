using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Rehably.API.Extensions;

/// <summary>
/// Registers NullableDateTimeModelBinder for all DateTime? form fields.
/// This ensures empty Swagger form inputs are treated as null rather than
/// causing a parse error that blocks the request.
/// </summary>
public class NullableDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(DateTime?) &&
            context.BindingInfo?.BindingSource == BindingSource.Form)
        {
            return new NullableDateTimeModelBinder();
        }

        return null;
    }
}
