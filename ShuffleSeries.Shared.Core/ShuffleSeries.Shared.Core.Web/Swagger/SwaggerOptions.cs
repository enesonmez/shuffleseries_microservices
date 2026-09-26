namespace ShuffleSeries.Shared.Core.Web.Swagger;

/// <summary>
/// OpenAPI, Swagger UI ve Scalar dokümantasyon yapılandırma seçenekleri.
/// </summary>
public sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";

    /// <summary>
    /// API Adı (örn. "ShuffleSeries Catalog API").
    /// </summary>
    public string Title { get; set; } = "ShuffleSeries API";

    /// <summary>
    /// API Sürümü (örn. "v1").
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// API Açıklaması.
    /// </summary>
    public string Description { get; set; } = "ShuffleSeries Microservices Platform API Documentation";

    /// <summary>
    /// JWT Bearer yetkilendirme şemasını (Authorize düğmesi) dokümana ekler.
    /// </summary>
    public bool IncludeJwtSecurity { get; set; } = true;

    /// <summary>
    /// OpenAPI JSON endpoint'ini (/openapi/v1.json) etkinleştirir.
    /// </summary>
    public bool EnableOpenApi { get; set; } = true;

    /// <summary>
    /// Swagger UI arayüzünü (/swagger) etkinleştirir.
    /// </summary>
    public bool EnableSwaggerUi { get; set; } = true;

    /// <summary>
    /// Modern Scalar API Reference arayüzünü (/scalar/v1) etkinleştirir.
    /// </summary>
    public bool EnableScalarUi { get; set; } = true;

    /// <summary>
    /// Swagger UI route prefix (varsayılan: "swagger").
    /// </summary>
    public string RoutePrefix { get; set; } = "swagger";
}
