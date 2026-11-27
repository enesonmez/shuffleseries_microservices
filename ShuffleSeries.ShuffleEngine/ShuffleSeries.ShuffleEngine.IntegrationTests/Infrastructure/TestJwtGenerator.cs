using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

public static class TestJwtGenerator
{
    public const string TestSecret = "DefaultDevelopmentJwtSecretKeyMustBeLongAndSecureEnough123!";
    public const string TestIssuer = "ShuffleSeries.Identity";
    public const string TestAudience = "ShuffleSeries.Microservices";

    public static string GenerateToken(
        Guid? userId = null,
        string role = SystemRoles.Admin,
        IEnumerable<string>? permissions = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        var perms = permissions ??
        [
            SystemPermissions.Shuffle.Basic,
            SystemPermissions.Shuffle.Vip
        ];

        foreach (var perm in perms)
        {
            claims.Add(new Claim("permission", perm));
        }

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
