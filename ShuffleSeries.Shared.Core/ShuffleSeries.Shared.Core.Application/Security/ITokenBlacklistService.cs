namespace ShuffleSeries.Shared.Core.Application.Security;

/// <summary>
/// Merkezi Redis tabanlı JWT Token Blacklist sözleşmesi.
/// Hem tekil token (jti) iptallerini hem de kullanıcı bazlı toplu iptalleri (userId + iat) yönetir.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Belirtilen jti (JWT ID) kimliğini kalan geçerlilik süresi kadar kara listeye alır.
    /// </summary>
    Task BlacklistTokenAsync(string jti, TimeSpan timeToLive, string reason = "revoked", CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen jti kimliğinin kara listede olup olmadığını sorgular.
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının o ana kadar üretilmiş tüm token'larını geçersiz kılmak için anlık damga atar.
    /// </summary>
    Task BlacklistUserTokensAsync(Guid userId, TimeSpan maxTokenLifetime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının verilen token'ının üretim tarihi (iat), kullanıcının iptal damgasından önce mi kontrol eder.
    /// </summary>
    Task<bool> IsUserBlacklistedAsync(Guid userId, DateTime tokenIssuedAtUtc, CancellationToken cancellationToken = default);
}
