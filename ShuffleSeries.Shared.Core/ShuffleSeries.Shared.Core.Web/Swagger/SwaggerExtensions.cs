using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace ShuffleSeries.Shared.Core.Web.Swagger;

/// <summary>
/// OpenAPI, Swagger UI ve Scalar altyapısı için genişletme metotları.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Mikroservis için merkezi OpenAPI ve Swagger yapılandırmasını DI konteynerine ekler.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configure">Swagger ve OpenAPI seçenekleri yapılandırma delegesi.</param>
    public static IServiceCollection AddSharedSwagger(
        this IServiceCollection services,
        Action<SwaggerOptions>? configure = null)
    {
        var options = new SwaggerOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddEndpointsApiExplorer();

        services.AddOpenApi(options.Version, openApiOptions =>
        {
            openApiOptions.AddDocumentTransformer(new OpenApiSecurityDocumentTransformer(options));
        });

        return services;
    }

    /// <summary>
    /// OpenAPI (/openapi/v1.json), Swagger UI (/swagger) ve Scalar (/scalar/v1) middleware'lerini HTTP pipeline'ına ekler.
    /// </summary>
    /// <param name="app">WebApplication nesnesi.</param>
    public static WebApplication UseSharedSwagger(this WebApplication app)
    {
        var options = app.Services.GetService<SwaggerOptions>() ?? new SwaggerOptions();

        if (options.EnableOpenApi)
        {
            app.MapOpenApi();
        }

        if (options.EnableSwaggerUi)
        {
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/openapi/{options.Version}.json", $"{options.Title} {options.Version}");
                c.RoutePrefix = options.RoutePrefix;
                c.DocumentTitle = $"{options.Title} - Swagger UI";
                c.DisplayRequestDuration();
                c.EnablePersistAuthorization();
            });
        }

        if (options.EnableScalarUi)
        {
            app.MapScalarApiReference(c =>
            {
                c.WithTitle(options.Title);
            });
        }

        return app;
    }
}
