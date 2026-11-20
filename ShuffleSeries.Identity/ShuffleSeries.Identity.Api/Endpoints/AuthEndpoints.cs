using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Identity.Api.Contracts.Requests.Auth;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.ChangePassword;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.ConvertGuest;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.DeleteAccount;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;
using ShuffleSeries.Identity.Application.Features.Auth.Queries.GetCurrentUser;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;
using ShuffleSeries.Shared.Core.Web.Extensions;
using Wolverine;

namespace ShuffleSeries.Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/auth")
            .WithTags("Authentication");

        // 1. POST: Register standard user
        group.MapPost("/register", async (
                [FromBody] RegisterRequest request,
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new RegisterCommand(request.Email, request.Password, ip);
                var response = await bus.InvokeAsync<AuthResponse>(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("Register")
            .WithSummary("Registers a new standard user with email and password")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 2. POST: Login with credentials
        group.MapPost("/login", async (
                [FromBody] LoginRequest request,
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new LoginCommand(request.Email, request.Password, ip);
                var response = await bus.InvokeAsync<AuthResponse>(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("Login")
            .WithSummary("Authenticates user credentials and returns JWT with refresh token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 3. POST: Refresh Token rotation
        group.MapPost("/refresh", async (
                [FromBody] RefreshTokenRequest request,
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new RefreshTokenCommand(request.RefreshToken, ip);
                var response = await bus.InvokeAsync<TokenResponse>(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("RefreshToken")
            .WithSummary("Rotates refresh token and issues a new access token")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 4. POST: Revoke Token (logout)
        group.MapPost("/revoke", async (
                [FromBody] RevokeTokenRequest request,
                ClaimsPrincipal user,
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var accessToken = request.AccessToken;
                if (string.IsNullOrWhiteSpace(accessToken) &&
                    httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    var headerVal = authHeader.ToString();
                    if (headerVal.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        accessToken = headerVal["Bearer ".Length..].Trim();
                    }
                }

                var jwtId = user.GetJwtId();
                var callerUserId = user.TryGetUserId();
                var command = new RevokeTokenCommand(request.RefreshToken, accessToken, jwtId, callerUserId);
                await bus.InvokeAsync<bool>(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RevokeToken")
            .WithSummary("Revokes a refresh token and blacklists the active access token")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 5. POST: Create Guest Session
        group.MapPost("/guest", async (
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var response = await bus.InvokeAsync<AuthResponse>(new CreateGuestSessionCommand(ip), cancellationToken);
                return Results.Ok(response);
            })
            .WithName("CreateGuestSession")
            .WithSummary("Creates an anonymous frictionless guest session with temporary JWT")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 6. POST: Social Login (Google / Apple SSO)
        group.MapPost("/social-login", async (
                [FromBody] SocialLoginRequest request,
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new SocialLoginCommand(request.Provider, request.IdToken, ip);
                var response = await bus.InvokeAsync<AuthResponse>(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("SocialLogin")
            .WithSummary("Authenticates via Apple or Google Single Sign-On ID Token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 7. POST: Convert Guest Account to Permanent Account (In-Place Elevation)
        group.MapPost("/convert-guest", async (
                [FromBody] ConvertGuestRequest request,
                ClaimsPrincipal user,
                HttpContext httpContext,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new ConvertGuestCommand(user.GetUserId(), request.Email, request.Password, ip, user.GetJwtId());
                var response = await bus.InvokeAsync<AuthResponse>(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("ConvertGuest")
            .WithSummary("Converts an active guest session to a permanent account preserving existing UserId")
            .RequireAuthorization()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 8. POST: Change Password
        group.MapPost("/change-password", async (
                [FromBody] ChangePasswordRequest request,
                ClaimsPrincipal user,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangePasswordCommand(user.GetUserId(), request.CurrentPassword, request.NewPassword);
                await bus.InvokeAsync<bool>(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("ChangePassword")
            .WithSummary("Changes the password for the current authenticated user and revokes active sessions")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesSecurityAndErrorProblems();

        // 9. GET: Get current user profile & effective permissions
        group.MapGet("/me", async (
                ClaimsPrincipal user,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var response = await bus.InvokeAsync<CurrentUserResponse>(new GetCurrentUserQuery(user.GetUserId()), cancellationToken);
                return Results.Ok(response);
            })
            .WithName("GetCurrentUser")
            .WithSummary("Gets current authenticated user details and resolved permissions")
            .RequireAuthorization()
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesSecurityAndErrorProblems();

        // 10. DELETE: Delete User Account (GDPR / Apple Guideline 5.1.1(v))
        group.MapDelete("/account", async (
                ClaimsPrincipal user,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                await bus.InvokeAsync<bool>(new DeleteAccountCommand(user.GetUserId()), cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteAccount")
            .WithSummary("Permanently soft-deletes the user account and publishes UserAccountDeletedEvent")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesSecurityAndErrorProblems();
    }

    private static string? GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString();
}
