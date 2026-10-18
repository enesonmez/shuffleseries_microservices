using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class EpisodePlatform : BaseEntity
{
    public Guid EpisodeId { get; private set; }
    public Guid PlatformId { get; private set; }
    public string WebUrl { get; private set; } = string.Empty;
    public string? AppSchemeUrl { get; private set; }

    public Platform? Platform { get; private set; }

    private EpisodePlatform() : base(Guid.Empty) { }

    internal EpisodePlatform(Guid episodeId, Guid platformId, string webUrl, string? appSchemeUrl = null)
        : base(Guid.Empty)
    {
        if (platformId == Guid.Empty)
            throw new ArgumentException("PlatformId cannot be empty.", nameof(platformId));

        if (string.IsNullOrWhiteSpace(webUrl))
            throw new ArgumentException("WebUrl cannot be empty.", nameof(webUrl));

        EpisodeId = episodeId;
        PlatformId = platformId;
        WebUrl = webUrl.Trim();
        AppSchemeUrl = string.IsNullOrWhiteSpace(appSchemeUrl) ? null : appSchemeUrl.Trim();
    }

    internal void UpdateUrls(string webUrl, string? appSchemeUrl = null)
    {
        if (string.IsNullOrWhiteSpace(webUrl))
            throw new ArgumentException("WebUrl cannot be empty.", nameof(webUrl));

        WebUrl = webUrl.Trim();
        AppSchemeUrl = string.IsNullOrWhiteSpace(appSchemeUrl) ? null : appSchemeUrl.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
