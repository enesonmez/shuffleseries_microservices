using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class GenreTranslation : BaseEntity
{
    public Guid GenreId { get; private set; }
    public LanguageCode LanguageCode { get; private set; } = LanguageCode.Turkish;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private GenreTranslation() : base(Guid.Empty)
    {
        LanguageCode = LanguageCode.Turkish;
    }

    internal GenreTranslation(Guid genreId, LanguageCode languageCode, string name, string? description = null)
        : base(Guid.Empty)
    {
        GenreId = genreId;
        LanguageCode = languageCode ?? throw new ArgumentNullException(nameof(languageCode));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    internal GenreTranslation(Guid genreId, string languageCode, string name, string? description = null)
        : this(genreId, LanguageCode.From(languageCode), name, description)
    {
    }

    internal void Update(string name, string? description = null)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }
}
