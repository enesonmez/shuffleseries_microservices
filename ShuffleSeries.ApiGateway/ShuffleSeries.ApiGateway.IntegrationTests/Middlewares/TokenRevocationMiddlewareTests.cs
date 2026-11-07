using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.ApiGateway.Api.Middlewares;

namespace ShuffleSeries.ApiGateway.IntegrationTests.Middlewares;

public sealed class TokenRevocationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenIsRevokedItemPresent_ShouldChallengeAndShortCircuit()
    {
        // Arrange
        var testAuthService = new TestAuthenticationService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(testAuthService)
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Items["IsRevoked"] = true;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new TokenRevocationMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse("Pipeline must be short-circuited when token is revoked");
        testAuthService.ChallengeCalled.Should().BeTrue("ChallengeAsync must be called on IAuthenticationService");
    }

    [Fact]
    public async Task InvokeAsync_WhenIsRevokedItemNotPresent_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new TokenRevocationMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue("Pipeline must proceed when token is not revoked");
    }

    private sealed class TestAuthenticationService : IAuthenticationService
    {
        public bool ChallengeCalled { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            ChallengeCalled = true;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;
    }
}
