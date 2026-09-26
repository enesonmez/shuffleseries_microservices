using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ShuffleSeries.Shared.Core.Web.Swagger;

/// <summary>
/// OpenAPI dokümanına API meta verilerini ve JWT Bearer yetkilendirme şemasını enjekte eden transformer.
/// </summary>
public sealed class OpenApiSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly SwaggerOptions _options;

    public OpenApiSecurityDocumentTransformer(SwaggerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = _options.Title;
        document.Info.Version = _options.Version;
        document.Info.Description = _options.Description;

        if (_options.IncludeJwtSecurity)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token format: Bearer {your_token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = securityScheme;

            var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);
            var requirement = new OpenApiSecurityRequirement
            {
                [schemeReference] = new List<string>()
            };

            document.Security ??= new List<OpenApiSecurityRequirement>();
            document.Security.Add(requirement);
        }

        return Task.CompletedTask;
    }
}
