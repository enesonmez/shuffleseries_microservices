using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Authorization;
using ShuffleSeries.Shared.Core.Web.Correlation;

namespace ShuffleSeries.Shared.Core.Web.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddSharedJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? throw new ArgumentNullException("Jwt:Secret is missing from configuration.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer is missing from configuration.");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience is missing from configuration.");

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
                    OnTokenValidated = context =>
                    {
                        // TODO: (Task 2.3) Burada Redis üzerinden Token Blacklist kontrolü yapılacak.
                        // Eğer token (jti) blacklist'te (Revoked) ise: context.Fail("Token is revoked.");
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
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

                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                            Title = "Unauthorized",
                            Detail = "Kimlik doğrulaması başarısız oldu. Geçerli bir Bearer token gereklidir.",
                            Instance = context.Request.Path
                        };

                        problemDetails.Extensions["code"] = "AUTH_UNAUTHORIZED";
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
                };
            });

        // 403 Forbidden durumlarında boş response dönmek yerine RFC 7807 ProblemDetails dönen custom result handler
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();

        services.AddAuthorization(options =>
        {
            // Rol tabanlı politikalar (Single Source of Truth: SystemRoles)
            options.AddPolicy(SystemPolicies.RequirePremiumRole, policy => policy.RequireRole(SystemRoles.Premium));
            options.AddPolicy(SystemPolicies.RequireAdminRole, policy => policy.RequireRole(SystemRoles.Admin));

            // İnce ayarlı izin tabanlı politikalar (Single Source of Truth: SystemPermissions)
            // Claim type "permission" JwtTokenService ile tam uyumludur.
            options.AddPolicy(SystemPolicies.CanReadCatalog, policy => policy.RequireClaim("permission", SystemPermissions.Catalog.Read));
            options.AddPolicy(SystemPolicies.CanCreateCatalog, policy => policy.RequireClaim("permission", SystemPermissions.Catalog.Create));
            options.AddPolicy(SystemPolicies.CanUpdateCatalog, policy => policy.RequireClaim("permission", SystemPermissions.Catalog.Update));
            options.AddPolicy(SystemPolicies.CanDeleteCatalog, policy => policy.RequireClaim("permission", SystemPermissions.Catalog.Delete));

            options.AddPolicy(SystemPolicies.CanExecuteBasicShuffle, policy => policy.RequireClaim("permission", SystemPermissions.Shuffle.Basic));
            options.AddPolicy(SystemPolicies.CanExecuteVipShuffle, policy => policy.RequireClaim("permission", SystemPermissions.Shuffle.Vip));

            options.AddPolicy(SystemPolicies.CanManageAuth, policy => policy.RequireClaim("permission", SystemPermissions.Auth.Manage));
        });

        return services;
    }
}
