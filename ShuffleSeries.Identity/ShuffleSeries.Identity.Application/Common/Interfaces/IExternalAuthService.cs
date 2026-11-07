using ShuffleSeries.Identity.Application.Common.Models;

namespace ShuffleSeries.Identity.Application.Common.Interfaces;

public interface IExternalAuthService
{
    Task<ExternalUserPrincipal?> VerifyTokenAsync(string provider, string idToken, CancellationToken cancellationToken = default);
}
