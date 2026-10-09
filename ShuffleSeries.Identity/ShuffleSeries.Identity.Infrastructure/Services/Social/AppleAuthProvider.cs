using Microsoft.Extensions.Options;
using ShuffleSeries.Identity.Infrastructure.Configuration;

namespace ShuffleSeries.Identity.Infrastructure.Services.Social;

/// <summary>
/// Social authentication provider for Apple Sign-In via Apple OpenID Connect JWKS.
/// </summary>
internal sealed class AppleAuthProvider : BaseJwtSocialAuthProvider
{
    private readonly ExternalAuthOptions _options;

    public AppleAuthProvider(HttpClient httpClient, IOptions<ExternalAuthOptions> options)
        : base(httpClient, options)
    {
        _options = options.Value;
    }

    public override string Provider => "Apple";

    protected override string JwksUri => "https://appleid.apple.com/auth/keys";

    protected override string[] ValidIssuers => ["https://appleid.apple.com"];

    protected override string? ClientId => _options.Apple.ClientId;
}
