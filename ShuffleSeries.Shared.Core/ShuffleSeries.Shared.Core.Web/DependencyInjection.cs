using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Web.Middlewares;

namespace ShuffleSeries.Shared.Core.Web;

public static class DependencyInjection
{
    public static void AddSharedExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    public static IApplicationBuilder UseSharedExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        return app;
    }
}