using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

/// <summary>
/// Bir misafir kullanıcının verileri kalıcı bir hesaba aktarıldığında (Data Merge) fırlatılan entegrasyon olayı.
/// Tüketenler:
/// - User Library: Misafir watchlist ve izleme kayıtlarını kalıcı hesaba aktarma.
/// - Ticket Economy: Kalan misafir biletlerini ve haklarını aktarma.
/// - History &amp; Analytics: Misafir oturumundaki telemetri verilerini kalıcı hesap ile eşleştirme.
/// </summary>
public interface IUserMergedEvent : IIntegrationEvent
{
    Guid TargetUserId { get; }
    Guid GuestUserId { get; }
}

public sealed record UserMergedEvent(
    Guid Id,
    Guid TargetUserId,
    Guid GuestUserId,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IUserMergedEvent
{
    public string EventType => nameof(UserMergedEvent);
}
