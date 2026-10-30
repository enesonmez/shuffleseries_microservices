using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ShuffleSeries.Shared.Core.Web.Extensions;

public static class EndpointConventionExtensions
{
    /// <summary>
    /// Minimal API endpoint'leri için standart RFC 7807 401 Unauthorized, 403 Forbidden ve 500 Internal Server Error OpenAPI yanıt tiplerini ekler.
    /// </summary>
    public static RouteHandlerBuilder ProducesSecurityAndErrorProblems(this RouteHandlerBuilder builder)
    {
        return builder
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
