using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

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

        var perms = permissions ?? [
            SystemPermissions.Catalog.MoviesRead,
            SystemPermissions.Catalog.MoviesCreate,
            SystemPermissions.Catalog.MoviesUpdate,
            SystemPermissions.Catalog.MoviesDelete,
            SystemPermissions.Catalog.SeriesRead,
            SystemPermissions.Catalog.SeriesCreate,
            SystemPermissions.Catalog.SeriesUpdate,
            SystemPermissions.Catalog.SeriesDelete,
            SystemPermissions.Catalog.EpisodesRead,
            SystemPermissions.Catalog.EpisodesCreate,
            SystemPermissions.Catalog.EpisodesUpdate,
            SystemPermissions.Catalog.EpisodesDelete,
            SystemPermissions.Catalog.PlatformsRead,
            SystemPermissions.Catalog.PlatformsCreate,
            SystemPermissions.Catalog.PlatformsUpdate,
            SystemPermissions.Catalog.PlatformsDelete,
            SystemPermissions.Catalog.MoodsRead,
            SystemPermissions.Catalog.MoodsCreate,
            SystemPermissions.Catalog.MoodsUpdate,
            SystemPermissions.Catalog.MoodsDelete,
            SystemPermissions.Catalog.GenresRead,
            SystemPermissions.Catalog.GenresCreate,
            SystemPermissions.Catalog.GenresUpdate,
            SystemPermissions.Catalog.GenresDelete,
            SystemPermissions.Catalog.AssociationsManage,
            SystemPermissions.Catalog.AdminMediaRead
        ];

        foreach (var p in perms)
        {
            claims.Add(new Claim("permission", p));
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
