using ShuffleSeries.Shared.Core.Web.Helpers;
using Yarp.ReverseProxy.Forwarder;

namespace ShuffleSeries.ApiGateway.Api.Extensions;

/// <summary>
/// API Gateway Reverse Proxy (YARP) pipeline yapılandırmalarını ve RFC 7807 hata dönüşümlerini yöneten genişletmeler.
/// </summary>
public static class ReverseProxyExtensions
{
    /// <summary>
    /// YARP Reverse Proxy yönlendirme hattını kaydeder ve erişilemeyen/zaman aşımına uğrayan mikroservis hatalarını
    /// RFC 7807 ProblemDetails formatına (DOWNSTREAM_SERVICE_UNAVAILABLE) dönüştürür.
    /// </summary>
    public static void MapGatewayReverseProxy(this WebApplication app)
    {
        app.MapReverseProxy(proxyPipeline =>
        {
            proxyPipeline.Use(async (context, next) =>
            {
                await next();
                await WriteForwarderErrorIfPresentAsync(context);
            });
        });
    }

    private static async Task WriteForwarderErrorIfPresentAsync(HttpContext context)
    {
        var errorFeature = context.GetForwarderErrorFeature();
        if (errorFeature is null || context.Response.HasStarted)
        {
            return;
        }

        var (statusCode, title) = errorFeature.Error switch
        {
            ForwarderError.NoAvailableDestinations => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable"),
            ForwarderError.RequestTimedOut => (StatusCodes.Status504GatewayTimeout, "Gateway Timeout"),
            _ => (StatusCodes.Status502BadGateway, "Bad Gateway")
        };

        await ProblemDetailsWriter.WriteAsync(
            context,
            new ProblemDetailsDescriptor(
                statusCode,
                title,
                "The upstream microservice is temporarily unreachable or taking too long to respond.",
                "DOWNSTREAM_SERVICE_UNAVAILABLE"));
    }
}
