using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Identity.Api.Contracts.Requests;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.DeleteAccount;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.MergeGuestAccount;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RevokeToken;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;
using ShuffleSeries.Identity.Application.Features.Auth.Queries.GetCurrentUser;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Web.Extensions;

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
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new RegisterCommand(request.Email, request.Password, ip);
                var response = await sender.Send(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("Register")
            .WithSummary("Registers a new standard user with email and password")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        // 2. POST: Login with credentials
        group.MapPost("/login", async (
                [FromBody] LoginRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new LoginCommand(request.Email, request.Password, ip);
                var response = await sender.Send(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("Login")
            .WithSummary("Authenticates user credentials and returns JWT with refresh token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 3. POST: Refresh Token rotation
        group.MapPost("/refresh", async (
                [FromBody] RefreshTokenRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new RefreshTokenCommand(request.RefreshToken, ip);
                var response = await sender.Send(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("RefreshToken")
            .WithSummary("Rotates refresh token and issues a new access token")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 4. POST: Revoke Token (logout)
        group.MapPost("/revoke", async (
                [FromBody] RevokeTokenRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RevokeTokenCommand(request.RefreshToken);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RevokeToken")
            .WithSummary("Revokes a refresh token")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 5. POST: Create Guest Session
        group.MapPost("/guest", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var response = await sender.Send(new CreateGuestSessionCommand(ip), cancellationToken);
                return Results.Ok(response);
            })
            .WithName("CreateGuestSession")
            .WithSummary("Creates an anonymous frictionless guest session with temporary JWT")
            .Produces<AuthResponse>(StatusCodes.Status200OK);

        // 6. POST: Social Login (Google / Apple SSO)
        group.MapPost("/social-login", async (
                [FromBody] SocialLoginRequest request,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var ip = GetClientIp(httpContext);
                var command = new SocialLoginCommand(request.Provider, request.IdToken, request.AppleRefreshToken, ip);
                var response = await sender.Send(command, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("SocialLogin")
            .WithSummary("Authenticates via Apple or Google Single Sign-On ID Token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 7. POST: Merge Guest Account
        group.MapPost("/merge-guest", async (
                [FromBody] MergeGuestRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var targetUserId = user.TryGetUserId() ?? request.TargetUserId ?? Guid.Empty;
                if (user.TryGetUserId() is { } authenticatedUserId && request.TargetUserId is not null && request.TargetUserId != Guid.Empty && request.TargetUserId != authenticatedUserId)
                {
                    throw new CannotMergeIntoDifferentUserException();
                }

                var command = new MergeGuestAccountCommand(request.GuestUserId, targetUserId);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("MergeGuestAccount")
            .WithSummary("Merges guest telemetry, watchlist, and tickets into permanent account")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 8. GET: Get current user profile & effective permissions
        group.MapGet("/me", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var response = await sender.Send(new GetCurrentUserQuery(user.GetUserId()), cancellationToken);
                return Results.Ok(response);
            })
            .WithName("GetCurrentUser")
            .WithSummary("Gets current authenticated user details and resolved permissions")
            .RequireAuthorization()
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 9. DELETE: Delete User Account (GDPR / Apple Guideline 5.1.1(v))
        group.MapDelete("/account", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteAccountCommand(user.GetUserId()), cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteAccount")
            .WithSummary("Permanently soft-deletes the user account and publishes UserAccountDeletedEvent")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static string? GetClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ip = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
