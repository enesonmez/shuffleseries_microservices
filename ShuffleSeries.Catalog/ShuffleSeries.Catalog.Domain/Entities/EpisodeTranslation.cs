using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class EpisodeTranslation : BaseEntity
{
    public Guid EpisodeId { get; private set; }
    public LanguageCode LanguageCode { get; private set; } = LanguageCode.Turkish;
    public string Title { get; private set; } = string.Empty;
    public string? Overview { get; private set; }

    private EpisodeTranslation() : base(Guid.Empty)
    {
        LanguageCode = LanguageCode.Turkish;
    }

    internal EpisodeTranslation(Guid episodeId, LanguageCode languageCode, string title, string? overview = null)
        : base(Guid.Empty)
    {
        EpisodeId = episodeId;
        LanguageCode = languageCode ?? throw new ArgumentNullException(nameof(languageCode));
        Title = title.Trim();
        Overview = string.IsNullOrWhiteSpace(overview) ? null : overview.Trim();
    }

    internal EpisodeTranslation(Guid episodeId, string languageCode, string title, string? overview = null)
        : this(episodeId, LanguageCode.From(languageCode), title, overview)
    {
    }

    internal void Update(string title, string? overview = null)
    {
        Title = title.Trim();
        Overview = string.IsNullOrWhiteSpace(overview) ? null : overview.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
