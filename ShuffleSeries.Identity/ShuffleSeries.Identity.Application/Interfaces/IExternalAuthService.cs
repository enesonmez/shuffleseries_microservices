using ShuffleSeries.Identity.Application.Models;

namespace ShuffleSeries.Identity.Application.Interfaces;

public interface IExternalAuthService
{
    Task<ExternalUserPrincipal?> VerifyTokenAsync(string provider, string idToken, CancellationToken cancellationToken = default);
}
