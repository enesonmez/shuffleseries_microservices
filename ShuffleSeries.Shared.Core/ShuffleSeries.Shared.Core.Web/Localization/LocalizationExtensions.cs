using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShuffleSeries.Shared.Core.Application.Common.Interfaces;

namespace ShuffleSeries.Shared.Core.Web.Localization;

public static class LocalizationExtensions
{
    /// <summary>
    /// Registers HttpContextAccessor and Scoped ILanguageContext implementation
    /// for resolving client language (?lang= query parameter and Accept-Language header).
    /// </summary>
    public static IServiceCollection AddSharedLocalization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<ILanguageContext, LanguageContext>();
        return services;
    }
}
