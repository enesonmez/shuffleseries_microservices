using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

/// <summary>
/// Yeni bir kullanıcı (Standart veya Misafir) kaydolduğunda mikroservis ekosistemine fırlatılan entegrasyon olayı.
/// Tüketenler:
/// - Notification Service: Hoş geldin e-postası gönderimi.
/// - Ticket Economy: 14 günlük balayı bilet kotası tanımlama ve Streak/XP profilini ilklendirme.
/// - Profile Service: Başlangıç kullanıcı profil kaydı ve varsayılan ayarların oluşturulması.
/// </summary>
public interface IUserRegisteredEvent : IIntegrationEvent
{
    Guid UserId { get; }
    string Email { get; }
    bool IsGuest { get; }
}

public sealed record UserRegisteredEvent(
    Guid Id,
    Guid UserId,
    string Email,
    bool IsGuest,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IUserRegisteredEvent
{
    public string EventType => nameof(UserRegisteredEvent);
}
