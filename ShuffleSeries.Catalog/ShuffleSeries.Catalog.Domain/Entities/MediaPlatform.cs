using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class MediaPlatform : BaseEntity
{
    public Guid MediaId { get; private set; }
    public Guid PlatformId { get; private set; }
    public string WebUrl { get; private set; } = string.Empty;
    public string? AppSchemeUrl { get; private set; }

    public Platform? Platform { get; private set; }

    private MediaPlatform() : base(Guid.Empty) { }

    internal MediaPlatform(Guid mediaId, Guid platformId, string webUrl, string? appSchemeUrl = null)
        : base(Guid.Empty)
    {
        if (platformId == Guid.Empty)
            throw new ArgumentException("PlatformId cannot be empty.", nameof(platformId));

        if (string.IsNullOrWhiteSpace(webUrl))
            throw new ArgumentException("WebUrl cannot be empty.", nameof(webUrl));

        MediaId = mediaId;
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
