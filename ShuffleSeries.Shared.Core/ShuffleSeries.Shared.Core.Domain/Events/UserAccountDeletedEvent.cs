using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

/// <summary>
/// Bir kullanıcı hesabı kalıcı olarak silindiğinde (Apple Guideline 5.1.1(v) ve GDPR/KVKK) fırlatılan entegrasyon olayı.
/// Tüketenler:
/// - Profile Service: Kullanıcı profilini ve tercihlerini temizleme/anonimleştirme.
/// - User Library: Watchlist ve izleme kütüphanesini temizleme.
/// - Ticket Economy: Bilet bakiyesi, streak ve rozet verilerini temizleme.
/// - Notification Service: APNs/FCM cihaz token'larını ve bildirim geçmişini silme.
/// - History &amp; Analytics: Kullanıcı telemetrisini "unutulma hakkı" uyarınca anonimleştirme.
/// </summary>
public interface IUserAccountDeletedEvent : IIntegrationEvent
{
    Guid UserId { get; }
    string? Email { get; }
}

public sealed record UserAccountDeletedEvent(
    Guid Id,
    Guid UserId,
    string? Email,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IUserAccountDeletedEvent
{
    public string EventType => nameof(UserAccountDeletedEvent);
}
