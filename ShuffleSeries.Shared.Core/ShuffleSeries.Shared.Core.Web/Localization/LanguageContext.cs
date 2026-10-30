using Microsoft.AspNetCore.Http;
using ShuffleSeries.Shared.Core.Application.Common.Interfaces;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Shared.Core.Web.Localization;

public sealed class LanguageContext : ILanguageContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _cachedLanguage;

    public LanguageContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentLanguage => _cachedLanguage ??= ResolveLanguage();

    private string ResolveLanguage()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return LanguageCode.Turkish.Value;
        }

        // 1. Query parameter ?lang=
        if (httpContext.Request.Query.TryGetValue("lang", out var queryLang) &&
            !string.IsNullOrWhiteSpace(queryLang))
        {
            var candidate = queryLang.ToString().Trim();
            if (LanguageCode.TryFrom(candidate, out var validCode))
            {
                return validCode.Value;
            }
        }

        // 2. Accept-Language header (e.g. "tr-TR,tr;q=0.9,en;q=0.8")
        var acceptHeader = httpContext.Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrWhiteSpace(acceptHeader))
        {
            var resolved = ResolveFromAcceptLanguageHeader(acceptHeader);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        // 3. Graceful Fallback
        return LanguageCode.Turkish.Value;
    }

    private static string? ResolveFromAcceptLanguageHeader(string acceptLanguage)
    {
        var tags = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var tag in tags)
        {
            var cleanTag = tag.Split(';')[0].Trim();
            if (string.IsNullOrWhiteSpace(cleanTag))
                continue;

            // Normalize "tr-TR" -> "tr", "en-US" -> "en"
            var primarySubtag = cleanTag.Split('-')[0].Trim();
            if (LanguageCode.TryFrom(primarySubtag, out var code))
            {
                return code.Value;
            }
        }

        return null;
    }
}
