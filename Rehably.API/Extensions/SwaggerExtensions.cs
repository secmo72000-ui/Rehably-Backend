using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Rehably.API.Extensions;

public static class SwaggerExtensions
{
    internal static readonly string[] PriorityTags = ["Auth - Login", "Auth - OTP"];

    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Rehably API",
                Version = "v1",
                Description = "Multi-tenant SaaS backend for physiotherapy clinic management"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.OrderActionsBy(apiDesc =>
            {
                var tag = apiDesc.ActionDescriptor.EndpointMetadata
                    .OfType<Microsoft.AspNetCore.Http.Metadata.ITagsMetadata>()
                    .FirstOrDefault()?.Tags.FirstOrDefault() ?? "zzz";
                var idx = Array.IndexOf(PriorityTags, tag);
                var prefix = idx >= 0 ? $"0_{idx:D2}" : $"1_{tag}";
                return $"{prefix}_{apiDesc.RelativePath}";
            });

            options.DocumentFilter<TagOrderDocumentFilter>();
            options.OperationFilter<FileUploadOperationFilter>();
            options.SchemaFilter<NullableDateTimeSchemaFilter>();

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Load Application layer XML docs (DTOs, validators, etc.)
            var appXmlPath = Path.Combine(AppContext.BaseDirectory, "Rehably.Application.xml");
            if (File.Exists(appXmlPath))
            {
                options.IncludeXmlComments(appXmlPath);
            }
        });

        return services;
    }

    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rehably API v1");
        });

        return app;
    }
}

file class TagOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var allTags = swaggerDoc.Paths
            .SelectMany(p => p.Value.Operations.SelectMany(o => o.Value.Tags))
            .Select(t => t.Name)
            .Distinct()
            .ToList();

        var ordered = SwaggerExtensions.PriorityTags
            .Where(allTags.Contains)
            .Concat(allTags.Except(SwaggerExtensions.PriorityTags).OrderBy(t => t))
            .ToList();

        swaggerDoc.Tags = ordered.Select(t => new OpenApiTag { Name = t }).ToList();
    }
}
