using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShuffleSeries.Identity.Api.Contracts.Requests;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Login;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.RefreshToken;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.Register;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Events;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;
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
    public async Task CreateGuestSession_ShouldReturnGuestToken_WithGuestClaims_AndPersistOutbox()
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

        await ExecuteDbContextAsync(async context =>
        {
            var outboxMessages = await context.OutboxMessages
                .Where(m => m.Type.Contains("UserRegisteredDomainEvent"))
                .ToListAsync();

            var guestOutbox = outboxMessages.FirstOrDefault(m => m.Content.Contains(authResult.UserId.ToString()));
            guestOutbox.Should().NotBeNull();
            var domainEvent = JsonSerializer.Deserialize<UserRegisteredDomainEvent>(guestOutbox!.Content);
            domainEvent.Should().NotBeNull();
            domainEvent!.IsGuest.Should().BeTrue();
            domainEvent.UserId.Should().Be(authResult.UserId);
        });
    }

    [Fact]
    public async Task ConvertGuest_WithValidPayload_ShouldReturn200_AndPersistUserRegisteredDomainEvent()
    {
        // Arrange: Create a guest session
        var guestResponse = await Client.PostAsync("api/auth/guest", null);
        guestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var guestResult = await guestResponse.Content.ReadFromJsonAsync<AuthResponse>();
        guestResult.Should().NotBeNull();

        // Act: Convert guest to permanent account using guest Bearer token
        var convertRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/convert-guest")
        {
            Content = JsonContent.Create(new ConvertGuestRequest("converted.guest@shuffleseries.com", "Password123!"))
        };
        convertRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guestResult!.AccessToken);
        var convertResponse = await Client.SendAsync(convertRequest);

        // Assert
        convertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var convertResult = await convertResponse.Content.ReadFromJsonAsync<AuthResponse>();
        convertResult.Should().NotBeNull();
        convertResult!.UserId.Should().Be(guestResult.UserId); // UserId is preserved!
        convertResult.Email.Should().Be("converted.guest@shuffleseries.com");
        convertResult.IsGuest.Should().BeFalse();
        convertResult.Roles.Should().Contain(ShuffleSeries.Shared.Core.Domain.Constants.SystemRoles.Standard);

        await ExecuteDbContextAsync(async context =>
        {
            // Verify user in DB is upgraded in-place
            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Id == guestResult.UserId);
            userInDb.Should().NotBeNull();
            userInDb!.IsGuest.Should().BeFalse();
            userInDb.Email.Should().Be("converted.guest@shuffleseries.com");
            userInDb.Status.Should().Be(ShuffleSeries.Identity.Domain.Enums.UserStatus.Active);

            // Verify UserRegisteredDomainEvent outbox message is persisted with IsGuest: false
            var outboxMessages = await context.OutboxMessages
                .Where(m => m.Type.Contains("UserRegisteredDomainEvent"))
                .ToListAsync();

            var convertOutbox = outboxMessages.FirstOrDefault(m => m.Content.Contains("converted.guest@shuffleseries.com"));
            convertOutbox.Should().NotBeNull();
            var domainEvent = JsonSerializer.Deserialize<UserRegisteredDomainEvent>(convertOutbox!.Content);
            domainEvent.Should().NotBeNull();
            domainEvent!.UserId.Should().Be(guestResult.UserId);
            domainEvent.IsGuest.Should().BeFalse();
        });
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_ShouldReturn204_AndAllowLoginWithNewPassword()
    {
        // Arrange: Register standard user
        var email = "changepassword.test@shuffleseries.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";

        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, oldPassword));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerResult.Should().NotBeNull();

        // Act: Change password with Bearer token
        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(oldPassword, newPassword))
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registerResult!.AccessToken);
        var changeResponse = await Client.SendAsync(changeRequest);

        // Assert
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify login with old password fails
        var oldLoginResponse = await Client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, oldPassword));
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify login with new password succeeds
        var newLoginResponse = await Client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, newPassword));
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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

    [Fact]
    public async Task PurgeExpiredRefreshTokensJob_WhenTokensExpiredOrRevokedOlderThan30Days_ShouldPurgeFromDatabase()
    {
        // Arrange
        var user = User.CreateStandard("purgert@shuffleseries.com", "hash");
        var oldExpiredDate = DateTime.UtcNow.AddDays(-35);
        var oldRevokedDate = DateTime.UtcNow.AddDays(-32);

        // Add 1 old expired token, 1 old revoked token, and 1 active recent token
        user.AddRefreshToken("hash_old_expired", oldExpiredDate);
        var oldRevokedToken = user.AddRefreshToken("hash_old_revoked", DateTime.UtcNow.AddDays(5));
        oldRevokedToken.Revoke(revokedAtUtc: oldRevokedDate);
        user.AddRefreshToken("hash_active", DateTime.UtcNow.AddDays(7));

        await ExecuteDbContextAsync(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });

        // Act: Execute PurgeExpiredRefreshTokensJob
        await ExecuteDbContextAsync(async context =>
        {
            var job = new PurgeExpiredRefreshTokensJob(context, TimeProvider.System, NullLogger<PurgeExpiredRefreshTokensJob>.Instance);
            await job.ExecuteAsync();
        });

        // Assert: Old tokens purged, active token retained
        await ExecuteDbContextAsync(async context =>
        {
            var tokens = await context.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
            tokens.Should().HaveCount(1);
            tokens.First().TokenHash.Should().Be("hash_active");
        });
    }
}
