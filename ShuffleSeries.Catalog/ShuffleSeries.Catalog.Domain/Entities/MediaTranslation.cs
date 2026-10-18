using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class MediaTranslation : BaseEntity
{
    public Guid MediaId { get; private set; }
    public LanguageCode LanguageCode { get; private set; } = LanguageCode.Turkish;
    public string Title { get; private set; } = string.Empty;
    public string Overview { get; private set; } = string.Empty;
    public string? Tagline { get; private set; }

    // EF Core için parametresiz constructor
    private MediaTranslation() : base(Guid.Empty)
    {
        LanguageCode = LanguageCode.Turkish;
    }

    internal MediaTranslation(Guid mediaId, LanguageCode languageCode, string title, string overview, string? tagline = null)
        : base(Guid.Empty)
    {
        MediaId = mediaId;
        LanguageCode = languageCode ?? throw new ArgumentNullException(nameof(languageCode));
        Title = title.Trim();
        Overview = overview.Trim();
        Tagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline.Trim();
    }

    internal MediaTranslation(Guid mediaId, string languageCode, string title, string overview, string? tagline = null)
        : this(mediaId, LanguageCode.From(languageCode), title, overview, tagline)
    {
    }

    internal void Update(string title, string overview, string? tagline = null)
    {
        Title = title.Trim();
        Overview = overview.Trim();
        Tagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
