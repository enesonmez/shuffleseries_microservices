using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Kullanıcıların rolleri (Admin, Premium, Standard, Guest) kayıt anında atanır ve 'role' claim'i üzerinden gelir.
            options.AddPolicy("RequirePremiumRole", policy => policy.RequireRole("Premium"));
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

            // Ayrıca spesifik özellikler için tanımlanan ince ayarlı (fine-grained) claim'ler (Örn: "catalog.read") 
            // kullanıcıya rolü dışında ekstra atanabilir veya çıkarılabilir.
            options.AddPolicy("CanReadCatalog", policy => policy.RequireClaim("Permission", "catalog.read"));
        });

        return services;
    }
}
