using System.Security.Claims;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Shared.Core.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("User ID could not be identified from the token claims.", "UNAUTHORIZED");
        }

        return userId;
    }

    public static Guid? TryGetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Email)?.Value
        ?? principal.FindFirst("email")?.Value;

    public static bool IsGuest(this ClaimsPrincipal principal) =>
        bool.TryParse(principal.FindFirst("is_guest")?.Value, out var isGuest) && isGuest;

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal) =>
        principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("role"))
            .Select(c => c.Value)
            .Distinct();

    public static IEnumerable<string> GetPermissions(this ClaimsPrincipal principal) =>
        principal.FindAll("permissions")
            .Select(c => c.Value)
            .Distinct();

    public static bool HasPermission(this ClaimsPrincipal principal, string permission) =>
        principal.FindAll("permissions")
            .Any(c => c.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));

    public static string? GetJwtId(this ClaimsPrincipal principal) =>
        principal.FindFirst("jti")?.Value
        ?? principal.FindFirst(ClaimTypes.SerialNumber)?.Value;

    public static DateTime? GetIssuedAtUtc(this ClaimsPrincipal principal)
    {
        var iatValue = principal.FindFirst("iat")?.Value;
        if (long.TryParse(iatValue, out var iatSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;
        }

        return null;
    }
}

