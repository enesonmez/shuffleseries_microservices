using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Authorization;
using ShuffleSeries.Shared.Core.Web.Correlation;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Shared.Core.Web.Authentication;

public static class AuthenticationExtensions
{
    private const string PermissionClaimType = "permission";

    public static IServiceCollection AddSharedJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing from configuration.");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing from configuration.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero // Sıkı güvenlik için (Sıfır tolerans)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = HandleTokenValidatedAsync,
                    OnChallenge = HandleJwtChallengeAsync
                };
            });

        // 403 Forbidden durumlarında boş response dönmek yerine RFC 7807 ProblemDetails dönen custom result handler
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();

        var authBuilder = services.AddAuthorizationBuilder();

        // Rol tabanlı politikalar (Single Source of Truth: SystemRoles)
        authBuilder.AddPolicy(SystemPolicies.RequirePremiumRole, policy => policy.RequireRole(SystemRoles.Premium));
        authBuilder.AddPolicy(SystemPolicies.RequireAdminRole, policy => policy.RequireRole(SystemRoles.Admin));

        // Granüler Movies Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanReadCatalogMovies, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoviesRead));
        authBuilder.AddPolicy(SystemPolicies.CanCreateCatalogMovies, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoviesCreate));
        authBuilder.AddPolicy(SystemPolicies.CanUpdateCatalogMovies, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoviesUpdate));
        authBuilder.AddPolicy(SystemPolicies.CanDeleteCatalogMovies, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoviesDelete));

        // Granüler Series Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanReadCatalogSeries, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.SeriesRead));
        authBuilder.AddPolicy(SystemPolicies.CanCreateCatalogSeries, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.SeriesCreate));
        authBuilder.AddPolicy(SystemPolicies.CanUpdateCatalogSeries, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.SeriesUpdate));
        authBuilder.AddPolicy(SystemPolicies.CanDeleteCatalogSeries, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.SeriesDelete));

        // Granüler Episodes Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanReadCatalogEpisodes, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.EpisodesRead));
        authBuilder.AddPolicy(SystemPolicies.CanCreateCatalogEpisodes, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.EpisodesCreate));
        authBuilder.AddPolicy(SystemPolicies.CanUpdateCatalogEpisodes, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.EpisodesUpdate));
        authBuilder.AddPolicy(SystemPolicies.CanDeleteCatalogEpisodes, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.EpisodesDelete));

        // Granüler Platforms Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanReadCatalogPlatforms, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.PlatformsRead));
        authBuilder.AddPolicy(SystemPolicies.CanCreateCatalogPlatforms, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.PlatformsCreate));
        authBuilder.AddPolicy(SystemPolicies.CanUpdateCatalogPlatforms, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.PlatformsUpdate));
        authBuilder.AddPolicy(SystemPolicies.CanDeleteCatalogPlatforms, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.PlatformsDelete));

        // Granüler Moods Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanReadCatalogMoods, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoodsRead));
        authBuilder.AddPolicy(SystemPolicies.CanCreateCatalogMoods, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoodsCreate));
        authBuilder.AddPolicy(SystemPolicies.CanUpdateCatalogMoods, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoodsUpdate));
        authBuilder.AddPolicy(SystemPolicies.CanDeleteCatalogMoods, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.MoodsDelete));

        // Granüler Genres Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanReadCatalogGenres, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.GenresRead));
        authBuilder.AddPolicy(SystemPolicies.CanCreateCatalogGenres, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.GenresCreate));
        authBuilder.AddPolicy(SystemPolicies.CanUpdateCatalogGenres, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.GenresUpdate));
        authBuilder.AddPolicy(SystemPolicies.CanDeleteCatalogGenres, policy =>
            policy.RequireClaim(PermissionClaimType, SystemPermissions.Catalog.GenresDelete));

        // Granüler Media & Episode Associations & Translations Politikası (Admin rolü veya catalog:associations:manage izni)
        authBuilder.AddPolicy(SystemPolicies.CanManageCatalogAssociations, policy =>
            policy.RequireAssertion(ctx => ctx.User.IsInRole(SystemRoles.Admin) || ctx.User.HasClaim(PermissionClaimType, SystemPermissions.Catalog.AssociationsManage)));

        // Admin Media CMS Politikası (Admin rolü veya catalog:admin:read izni)
        authBuilder.AddPolicy(SystemPolicies.CanReadAdminMedia, policy =>
            policy.RequireAssertion(ctx => ctx.User.IsInRole(SystemRoles.Admin) || ctx.User.HasClaim(PermissionClaimType, SystemPermissions.Catalog.AdminMediaRead)));

        // Shuffle Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanExecuteBasicShuffle, policy => policy.RequireClaim(PermissionClaimType, SystemPermissions.Shuffle.Basic));
        authBuilder.AddPolicy(SystemPolicies.CanExecuteVipShuffle, policy => policy.RequireClaim(PermissionClaimType, SystemPermissions.Shuffle.Vip));

        // Auth Politikaları
        authBuilder.AddPolicy(SystemPolicies.CanManageAuth, policy => policy.RequireClaim(PermissionClaimType, SystemPermissions.Auth.Manage));

        return services;
    }

    private static async Task HandleTokenValidatedAsync(TokenValidatedContext context)
    {
        var blacklistService = context.HttpContext.RequestServices.GetService<ITokenBlacklistService>();
        if (blacklistService is null || context.Principal is null)
        {
            return;
        }

        var jti = context.Principal.GetJwtId();
        if (!string.IsNullOrEmpty(jti) && await blacklistService.IsTokenBlacklistedAsync(jti, context.HttpContext.RequestAborted))
        {
            context.HttpContext.Items["IsRevoked"] = true;
            context.Fail("Token has been revoked.");
            return;
        }

        var userId = context.Principal.TryGetUserId();
        var issuedAtUtc = context.Principal.GetIssuedAtUtc();
        if (userId.HasValue && issuedAtUtc.HasValue &&
            await blacklistService.IsUserBlacklistedAsync(userId.Value, issuedAtUtc.Value, context.HttpContext.RequestAborted))
        {
            context.HttpContext.Items["IsRevoked"] = true;
            context.Fail("User session has been revoked.");
        }
    }

    private static async Task HandleJwtChallengeAsync(JwtBearerChallengeContext context)
    {
        if (context.Handled)
        {
            return;
        }

        // Varsayılan boş 401 yanıtını ez ve RFC 7807 ProblemDetails üret
        context.HandleResponse();

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        var correlationContext = context.HttpContext.RequestServices.GetService<ICorrelationIdContext>();
        var correlationId = correlationContext?.CorrelationId;

        var isRevoked = context.HttpContext.Items.ContainsKey("IsRevoked")
                        || context.AuthenticateFailure?.Message?.Contains("revoked", StringComparison.OrdinalIgnoreCase) == true
                        || context.ErrorDescription?.Contains("revoked", StringComparison.OrdinalIgnoreCase) == true;

        var errorCode = isRevoked ? "AUTH_TOKEN_REVOKED" : "AUTH_UNAUTHORIZED";
        var detail = isRevoked
            ? "Sunulan kimlik doğrulama belirteci iptal edilmiştir (Revoked/Blacklisted)."
            : "Kimlik doğrulaması başarısız oldu. Geçerli bir Bearer token gereklidir.";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = isRevoked ? "Token Revoked" : "Unauthorized",
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["code"] = errorCode;
        problemDetails.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        if (!string.IsNullOrEmpty(context.Error))
        {
            problemDetails.Extensions["error"] = context.Error;
        }

        if (!string.IsNullOrEmpty(context.ErrorDescription))
        {
            problemDetails.Extensions["errorDescription"] = context.ErrorDescription;
        }

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.HttpContext.RequestAborted);
    }
}
