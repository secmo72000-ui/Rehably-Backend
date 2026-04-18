using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rehably.API.Extensions;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Remove date-time format from nullable DateTime form properties so Swagger
        // renders them as plain text inputs instead of auto-filling with current timestamp.
        if (operation.RequestBody?.Content != null
            && operation.RequestBody.Content.TryGetValue("multipart/form-data", out var formContent)
            && formContent.Schema?.Properties != null)
        {
            ClearDateTimeFormatFromFormSchema(formContent.Schema);
        }

        // Only handle standalone IFormFile parameters (not properties inside a [FromForm] model).
        // When IFormFile is a property of a complex model (e.g. CreateClinicRequest),
        // Swashbuckle already generates the correct multipart/form-data schema.
        var standaloneFileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source?.Id == "Form"
                        && (p.Type == typeof(IFormFile) || p.Type == typeof(IFormFileCollection)))
            .ToList();

        if (!standaloneFileParams.Any()) return;

        // If there's already a request body (from a [FromForm] model), merge file fields into it
        if (operation.RequestBody?.Content != null
            && operation.RequestBody.Content.TryGetValue("multipart/form-data", out var existing)
            && existing.Schema?.Properties != null)
        {
            foreach (var fp in standaloneFileParams)
            {
                existing.Schema.Properties[fp.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = $"Upload {fp.Name}"
                };
            }

            // Remove the standalone file params from the parameters list since they're now in the body
            var fileParamNames = standaloneFileParams.Select(p => p.Name).ToHashSet();
            operation.Parameters = operation.Parameters
                .Where(p => !fileParamNames.Contains(p.Name))
                .ToList();
            return;
        }

        // No existing request body — build a schema with all form fields + file fields
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>()
        };

        foreach (var param in context.ApiDescription.ParameterDescriptions.Where(p => p.Source?.Id == "Form"))
        {
            if (param.Type == typeof(IFormFile) || param.Type == typeof(IFormFileCollection))
            {
                schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = $"Upload {param.Name}"
                };
            }
            else
            {
                schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = GetOpenApiType(param.Type),
                    Description = param.ModelMetadata?.Description
                };
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType { Schema = schema }
            }
        };

        // Remove form params from parameters list since they're in the request body
        var formParamNames = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source?.Id == "Form")
            .Select(p => p.Name)
            .ToHashSet();

        operation.Parameters = operation.Parameters
            .Where(p => !formParamNames.Contains(p.Name))
            .ToList();
    }

    private static string GetOpenApiType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(int) || underlying == typeof(long)) return "integer";
        if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float)) return "number";
        if (underlying == typeof(bool)) return "boolean";
        if (underlying == typeof(Guid)) return "string";
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) return "string";
        if (underlying.IsEnum) return "integer";
        return "string";
    }

    /// <summary>
    /// Removes the date-time format from nullable DateTime form fields so Swagger
    /// renders them as plain text inputs instead of auto-filling with the current timestamp.
    /// </summary>
    public static void ClearDateTimeFormatFromFormSchema(OpenApiSchema schema)
    {
        foreach (var prop in schema.Properties.Values)
        {
            if (prop.Type == "string" && prop.Format == "date-time" && prop.Nullable)
                prop.Format = null;
        }
    }
}
