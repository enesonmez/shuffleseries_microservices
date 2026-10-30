using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class MoodTranslation : BaseEntity
{
    public Guid MoodId { get; private set; }
    public LanguageCode LanguageCode { get; private set; } = LanguageCode.Turkish;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? VibeTags { get; private set; }

    private MoodTranslation() : base(Guid.Empty)
    {
        LanguageCode = LanguageCode.Turkish;
    }

    internal MoodTranslation(Guid moodId, LanguageCode languageCode, string name, string description, string? vibeTags = null)
        : base(Guid.Empty)
    {
        MoodId = moodId;
        LanguageCode = languageCode ?? throw new ArgumentNullException(nameof(languageCode));
        Name = name.Trim();
        Description = description.Trim();
        VibeTags = string.IsNullOrWhiteSpace(vibeTags) ? null : vibeTags.Trim();
    }

    internal MoodTranslation(Guid moodId, string languageCode, string name, string description, string? vibeTags = null)
        : this(moodId, LanguageCode.From(languageCode), name, description, vibeTags)
    {
    }

    internal void Update(string name, string description, string? vibeTags = null)
    {
        Name = name.Trim();
        Description = description.Trim();
        VibeTags = string.IsNullOrWhiteSpace(vibeTags) ? null : vibeTags.Trim();
    }
}
