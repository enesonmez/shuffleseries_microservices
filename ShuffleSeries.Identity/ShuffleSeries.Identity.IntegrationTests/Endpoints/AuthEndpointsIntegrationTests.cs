using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.IntegrationTests.Infrastructure;

namespace ShuffleSeries.Identity.IntegrationTests.Endpoints;

public class AuthEndpointsIntegrationTests : IdentityIntegrationTestBase
{
    public AuthEndpointsIntegrationTests(IdentityApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_WithValidPayload_ShouldReturn200_AndPersistUserWithRolesAndOutbox()
    {
        // Arrange
        var command = new RegisterCommand("newuser@shuffleseries.com", "Password123!");

        // Act
        var response = await Client.PostAsJsonAsync("api/auth/register", command);

        // Assert HTTP Response
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: content);

        var authResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResult.Should().NotBeNull();
        authResult!.Email.Should().Be("newuser@shuffleseries.com");
        authResult.Roles.Should().Contain("Standard");
        authResult.Permissions.Should().Contain("catalog:read");
        authResult.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResult.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Assert Real PostgreSQL Database State
        await ExecuteDbContextAsync(async context =>
        {
            var userInDb = await context.Users
                .Include(u => u.UserRoles)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == authResult.UserId);

            userInDb.Should().NotBeNull();
            userInDb!.Email.Should().Be("newuser@shuffleseries.com");
            userInDb.UserRoles.Should().NotBeEmpty();
            userInDb.RefreshTokens.Should().NotBeEmpty();

            // Outbox message verification
            var outboxMessage = await context.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type.Contains("UserRegisteredDomainEvent"));
            outboxMessage.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200_AndValidTokens()
    {
        // Arrange: Register user first
        var registerCommand = new RegisterCommand("loginuser@shuffleseries.com", "Password123!");
        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", registerCommand);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Login
        var loginCommand = new LoginCommand("loginuser@shuffleseries.com", "Password123!");
        var loginResponse = await Client.PostAsJsonAsync("api/auth/login", loginCommand);

        // Assert
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: loginContent);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResult.Should().NotBeNull();
        authResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401Unauthorized()
    {
        // Arrange: Register user first
        var registerCommand = new RegisterCommand("wrongpass@shuffleseries.com", "Password123!");
        await Client.PostAsJsonAsync("api/auth/register", registerCommand);

        // Act: Login with wrong password
        var loginCommand = new LoginCommand("wrongpass@shuffleseries.com", "IncorrectPassword123!");
        var loginResponse = await Client.PostAsJsonAsync("api/auth/login", loginCommand);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldRotateAndReturnNewTokens()
    {
        // Arrange: Register user
        var registerCommand = new RegisterCommand("refreshuser@shuffleseries.com", "Password123!");
        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", registerCommand);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act: Refresh
        var refreshCommand = new RefreshTokenCommand(authResult!.RefreshToken);
        var refreshResponse = await Client.PostAsJsonAsync("api/auth/refresh", refreshCommand);

        // Assert
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: refreshContent);
        var tokenResult = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull();
        tokenResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResult.RefreshToken.Should().NotBe(authResult.RefreshToken);
    }

    [Fact]
    public async Task CreateGuestSession_ShouldReturnGuestToken_WithGuestClaims()
    {
        // Act
        var response = await Client.PostAsync("api/auth/guest", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResult.Should().NotBeNull();
        authResult!.IsGuest.Should().BeTrue();
        authResult.Roles.Should().Contain("Guest");
        authResult.Permissions.Should().Contain("shuffle:basic");
    }

    [Fact]
    public async Task GetCurrentUser_WithValidBearerToken_ShouldReturnProfile()
    {
        // Arrange: Register
        var registerCommand = new RegisterCommand("profileuser@shuffleseries.com", "Password123!");
        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", registerCommand);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("profileuser@shuffleseries.com");
        profile.Roles.Should().Contain("Standard");
    }

    [Fact]
    public async Task DeleteAccount_WithValidBearerToken_ShouldSoftDelete_AndWriteOutboxEvent()
    {
        // Arrange: Register
        var registerCommand = new RegisterCommand("deleteuser@shuffleseries.com", "Password123!");
        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", registerCommand);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Delete, "api/auth/account");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in DB
        await ExecuteDbContextAsync(async context =>
        {
            // Query ignoring query filter to verify soft delete
            var userInDb = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == authResult.UserId);

            userInDb.Should().NotBeNull();
            userInDb!.IsDeleted.Should().BeTrue();

            var outboxMessage = await context.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type.Contains("UserAccountDeletedDomainEvent"));
            outboxMessage.Should().NotBeNull();
        });
    }
}
