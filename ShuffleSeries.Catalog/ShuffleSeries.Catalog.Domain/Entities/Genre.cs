using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Genre : AggregateRoot
{
    private readonly List<GenreTranslation> _translations = [];

    public string Code { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<GenreTranslation> Translations => _translations.AsReadOnly();

    private Genre() : base(Guid.NewGuid()) { }

    private Genre(
        Guid id,
        string code,
        int displayOrder,
        bool isActive) : base(id)
    {
        Code = NormalizeCode(code);
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public static Genre Create(
        string code,
        int displayOrder = 0,
        bool isActive = true)
    {
        var genre = new Genre(
            Guid.NewGuid(),
            code,
            displayOrder,
            isActive);

        genre.RaiseDomainEvent(new GenreCreatedDomainEvent(genre.Id));

        return genre;
    }

    public void Update(
        string code,
        int displayOrder = 0)
    {
        Code = NormalizeCode(code);
        DisplayOrder = displayOrder;

        RaiseUpdatedEvent();
    }

    public GenreTranslation AddOrUpdateTranslation(
        LanguageCode languageCode,
        string name,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(languageCode);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Genre name cannot be empty.", nameof(name));

        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);

        if (existing is not null)
        {
            existing.Update(name, description);
            RaiseUpdatedEvent();
            return existing;
        }

        var translation = new GenreTranslation(Id, languageCode, name, description);
        _translations.Add(translation);
        RaiseUpdatedEvent();

        return translation;
    }

    public GenreTranslation AddOrUpdateTranslation(
        string languageCode,
        string name,
        string? description = null)
    {
        return AddOrUpdateTranslation(LanguageCode.From(languageCode), name, description);
    }

    public void RemoveTranslation(LanguageCode languageCode)
    {
        if (languageCode is null)
            return;

        foreach (var item in _translations.Where(t => t.LanguageCode == languageCode))
        {
            item.HardDelete();
        }

        var removed = _translations.RemoveAll(t => t.LanguageCode == languageCode);
        if (removed > 0)
        {
            RaiseUpdatedEvent();
        }
    }

    public void RemoveTranslation(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        if (LanguageCode.TryFrom(languageCode, out var parsed))
            RemoveTranslation(parsed);
    }

    public void Delete(string? deletedBy = null)
    {
        SoftDelete(deletedBy);
        foreach (var translation in _translations)
        {
            translation.SoftDelete(deletedBy);
        }
        RaiseDomainEvent(new GenreDeletedDomainEvent(Id));
    }

    private void RaiseUpdatedEvent()
    {
        if (GetDomainEvents().Any(e => e is GenreCreatedDomainEvent or GenreUpdatedDomainEvent))
            return;

        RaiseDomainEvent(new GenreUpdatedDomainEvent(Id));
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Genre code cannot be empty.", nameof(code));

        return code.Trim().ToLowerInvariant();
    }
}
