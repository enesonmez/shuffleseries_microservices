using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Application.Common.Interfaces;

/// <summary>
/// Başarılı kimlik doğrulama sonrasında refresh token üretimi, veritabanına kaydı,
/// efektif rollerin/izinlerin çözümlenmesi ve JWT üretimiyle nihai AuthResponse nesnesini oluşturan servis sözleşmesi.
/// </summary>
public interface IAuthResponseService
{
    Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
