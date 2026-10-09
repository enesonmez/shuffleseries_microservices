using Microsoft.Extensions.Options;
using ShuffleSeries.Identity.Infrastructure.Configuration;

namespace ShuffleSeries.Identity.Infrastructure.Services.Social;

/// <summary>
/// Social authentication provider for Google Sign-In via Google OpenID Connect JWKS.
/// </summary>
internal sealed class GoogleAuthProvider : BaseJwtSocialAuthProvider
{
    private readonly ExternalAuthOptions _options;

    public GoogleAuthProvider(HttpClient httpClient, IOptions<ExternalAuthOptions> options)
        : base(httpClient, options)
    {
        _options = options.Value;
    }

    public override string Provider => "Google";

    protected override string JwksUri => "https://www.googleapis.com/oauth2/v3/certs";

    protected override string[] ValidIssuers => ["https://accounts.google.com", "accounts.google.com"];

    protected override string? ClientId => _options.Google.ClientId;
}
