using System.Text.RegularExpressions;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Domain.Entities;

public partial class Mood : AggregateRoot
{
    private readonly List<MoodTranslation> _translations = [];

    [GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
    private static partial Regex HexColorRegex();

    public string Code { get; private set; } = string.Empty;
    public string AmbientColorHex { get; private set; } = string.Empty;
    public string? IconUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<MoodTranslation> Translations => _translations.AsReadOnly();

    private Mood() : base(Guid.NewGuid()) { }

    private Mood(
        Guid id,
        string code,
        string ambientColorHex,
        string? iconUrl,
        int displayOrder,
        bool isActive) : base(id)
    {
        Code = NormalizeCode(code);
        AmbientColorHex = NormalizeColorHex(ambientColorHex);
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public static Mood Create(
        string code,
        string ambientColorHex,
        string? iconUrl = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        var mood = new Mood(
            Guid.NewGuid(),
            code,
            ambientColorHex,
            iconUrl,
            displayOrder,
            isActive);

        mood.RaiseDomainEvent(new MoodCreatedDomainEvent(mood.Id));

        return mood;
    }

    public void Update(
        string code,
        string ambientColorHex,
        string? iconUrl = null,
        int displayOrder = 0)
    {
        Code = NormalizeCode(code);
        AmbientColorHex = NormalizeColorHex(ambientColorHex);
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        DisplayOrder = displayOrder;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseUpdatedEvent();
    }

    public MoodTranslation AddOrUpdateTranslation(
        LanguageCode languageCode,
        string name,
        string description,
        string? vibeTags = null)
    {
        ArgumentNullException.ThrowIfNull(languageCode);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mood name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Mood description cannot be empty.", nameof(description));

        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);

        if (existing is not null)
        {
            existing.Update(name, description, vibeTags);
            ModifiedAtUtc = DateTime.UtcNow;
            RaiseUpdatedEvent();
            return existing;
        }

        var translation = new MoodTranslation(Id, languageCode, name, description, vibeTags);
        _translations.Add(translation);
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();

        return translation;
    }

    public MoodTranslation AddOrUpdateTranslation(
        string languageCode,
        string name,
        string description,
        string? vibeTags = null)
    {
        return AddOrUpdateTranslation(LanguageCode.From(languageCode), name, description, vibeTags);
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
            ModifiedAtUtc = DateTime.UtcNow;
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

    public void ToggleActive(bool isActive)
    {
        IsActive = isActive;
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseUpdatedEvent();
    }

    public void Delete(string? deletedBy = null)
    {
        SoftDelete(deletedBy);
        RaiseDomainEvent(new MoodDeletedDomainEvent(Id));
    }

    private void RaiseUpdatedEvent()
    {
        if (GetDomainEvents().Any(e => e is MoodCreatedDomainEvent or MoodUpdatedDomainEvent))
            return;

        RaiseDomainEvent(new MoodUpdatedDomainEvent(Id));
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mood code cannot be empty.", nameof(code));

        return code.Trim().ToLowerInvariant();
    }

    private static string NormalizeColorHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("Ambient color hex cannot be empty.", nameof(hex));

        var trimmed = hex.Trim();
        var withHash = trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";

        if (!HexColorRegex().IsMatch(withHash))
            throw new ArgumentException($"'{hex}' is not a valid hex color code.", nameof(hex));

        return withHash.ToUpperInvariant();
    }
}
