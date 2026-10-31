using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Api.Contracts.Requests;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.IntegrationTests.Infrastructure;

namespace ShuffleSeries.Identity.IntegrationTests.Endpoints;

public class RealInfrastructureIntegrationTests : IdentityIntegrationTestBase
{
    public RealInfrastructureIntegrationTests(IdentityApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task OutboxJob_ShouldPublishUserRegisteredEventToRealRabbitMQ_AndMarkProcessedInRealPostgres()
    {
        // 1. Arrange: Yeni bir kullanıcı kaydet (PostgreSQL OutboxMessages tablosuna UserRegisteredDomainEvent yazılır)
        var registerRequest = new RegisterRequest("realinfra.user@shuffleseries.com", "Password123!");
        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Assert: Outbox tablosunda henüz işlenmemiş mesajın varlığını doğrula
        await ExecuteDbContextAsync(async db =>
        {
            var pendingMessage = await db.OutboxMessages
                .FirstOrDefaultAsync(m => m.ProcessedOnUtc == null && m.Type.Contains("UserRegisteredDomainEvent"));

            pendingMessage.Should().NotBeNull();
            pendingMessage!.ProcessedOnUtc.Should().BeNull();
            pendingMessage.Error.Should().BeNull();
        });

        // 3. Act: Outbox job'ını deterministik olarak on-demand tetikle
        // (Gerçek Testcontainers RabbitMQ'ya mesaj publish edilir ve PostgreSQL güncellenir)
        await TriggerOutboxJobAsync();

        // 4. Assert: Mesajın başarıyla işlendiğini ve ProcessedOnUtc'nin dolduğunu doğrula
        await ExecuteDbContextAsync(async db =>
        {
            var processedMessage = await db.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type.Contains("UserRegisteredDomainEvent"));

            processedMessage.Should().NotBeNull();
            processedMessage!.ProcessedOnUtc.Should().NotBeNull();
            processedMessage.Error.Should().BeNull();
            processedMessage.RetryCount.Should().Be(0);
        });
    }

    [Fact]
    public async Task ChangePassword_ShouldBlacklistUserInRealRedis_AndFlushOnReset()
    {
        // 1. Arrange: Kullanıcı oluştur ve token al
        var email = "redisblacklist.test@shuffleseries.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";

        var registerResponse = await Client.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, oldPassword));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResult.Should().NotBeNull();

        // 2. Act: Şifre değiştir (Redis üzerinde kullanıcının önceki oturumları blacklist'e alınır)
        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(oldPassword, newPassword))
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var changeResponse = await Client.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. Assert: Gerçek Redis container'ında blacklist anahtarının oluştuğunu doğrula
        var redisDb = Factory.Redis.GetDatabase();
        var blacklistKey = $"shuffleseries:blacklist:user:{authResult.UserId:N}";
        var isBlacklisted = await redisDb.KeyExistsAsync(blacklistKey);
        isBlacklisted.Should().BeTrue();

        // 4. ResetState: Test izolasyonu ile Redis'in temizlendiğini doğrula
        await Factory.ResetStateAsync();
        var keyExistsAfterReset = await redisDb.KeyExistsAsync(blacklistKey);
        keyExistsAfterReset.Should().BeFalse();
    }
}
