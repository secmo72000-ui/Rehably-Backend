using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rehably.API.Extensions;

/// <summary>
/// Removes date-time format from nullable DateTime schema properties so
/// Swagger UI renders them as plain text inputs instead of auto-filling
/// with the current timestamp.
/// </summary>
public class NullableDateTimeSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(DateTime?) && context.Type != typeof(DateTimeOffset?))
            return;

        schema.Format = null;
    }
}
