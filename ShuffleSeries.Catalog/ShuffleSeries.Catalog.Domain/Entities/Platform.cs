using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Platform : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string LogoUrl { get; private set; } = string.Empty;
    public string InactiveLogoUrl { get; private set; } = string.Empty;
    public string BaseWebUrl { get; private set; } = string.Empty;
    public string? BaseAppSchemeUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private Platform() : base(Guid.NewGuid()) { }

    private Platform(
        Guid id,
        string name,
        string slug,
        string logoUrl,
        string inactiveLogoUrl,
        string baseWebUrl,
        string? baseAppSchemeUrl,
        int displayOrder,
        bool isActive) : base(id)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        LogoUrl = logoUrl.Trim();
        InactiveLogoUrl = inactiveLogoUrl.Trim();
        BaseWebUrl = baseWebUrl.Trim();
        BaseAppSchemeUrl = string.IsNullOrWhiteSpace(baseAppSchemeUrl) ? null : baseAppSchemeUrl.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public static Platform Create(
        string name,
        string slug,
        string logoUrl,
        string inactiveLogoUrl,
        string baseWebUrl,
        string? baseAppSchemeUrl = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Platform name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Platform slug cannot be empty.", nameof(slug));

        if (string.IsNullOrWhiteSpace(logoUrl))
            throw new ArgumentException("Logo URL cannot be empty.", nameof(logoUrl));

        if (string.IsNullOrWhiteSpace(inactiveLogoUrl))
            throw new ArgumentException("Inactive logo URL cannot be empty.", nameof(inactiveLogoUrl));

        if (string.IsNullOrWhiteSpace(baseWebUrl))
            throw new ArgumentException("Base web URL cannot be empty.", nameof(baseWebUrl));

        var platform = new Platform(
            Guid.NewGuid(),
            name,
            slug,
            logoUrl,
            inactiveLogoUrl,
            baseWebUrl,
            baseAppSchemeUrl,
            displayOrder,
            isActive);

        platform.RaiseDomainEvent(new PlatformCreatedDomainEvent(platform.Id));

        return platform;
    }

    public void Update(
        string name,
        string slug,
        string logoUrl,
        string inactiveLogoUrl,
        string baseWebUrl,
        string? baseAppSchemeUrl = null,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Platform name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Platform slug cannot be empty.", nameof(slug));

        if (string.IsNullOrWhiteSpace(logoUrl))
            throw new ArgumentException("Logo URL cannot be empty.", nameof(logoUrl));

        if (string.IsNullOrWhiteSpace(inactiveLogoUrl))
            throw new ArgumentException("Inactive logo URL cannot be empty.", nameof(inactiveLogoUrl));

        if (string.IsNullOrWhiteSpace(baseWebUrl))
            throw new ArgumentException("Base web URL cannot be empty.", nameof(baseWebUrl));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        LogoUrl = logoUrl.Trim();
        InactiveLogoUrl = inactiveLogoUrl.Trim();
        BaseWebUrl = baseWebUrl.Trim();
        BaseAppSchemeUrl = string.IsNullOrWhiteSpace(baseAppSchemeUrl) ? null : baseAppSchemeUrl.Trim();
        DisplayOrder = displayOrder;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseUpdatedEvent();
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
        RaiseDomainEvent(new PlatformDeletedDomainEvent(Id));
    }

    private void RaiseUpdatedEvent()
    {
        if (GetDomainEvents().Any(e => e is PlatformCreatedDomainEvent or PlatformUpdatedDomainEvent))
            return;

        RaiseDomainEvent(new PlatformUpdatedDomainEvent(Id));
    }
}
