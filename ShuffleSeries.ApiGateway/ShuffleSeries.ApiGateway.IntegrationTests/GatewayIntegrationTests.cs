using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ShuffleSeries.ApiGateway.IntegrationTests;

public class GatewayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Minimal API WebApplication.CreateBuilder() anında çalıştığı için
        // in-memory collection yerine Environment variable ile ezmek daha güvenlidir.
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration_test_dummy_secret_key_long_enough");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _factory = factory;
    }

    [Fact]
    public async Task RateLimiting_StrictLimiter_ShouldReturn429_WhenLimitExceeded()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/api/auth/dummy-endpoint");
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var exceededResponse = await client.GetAsync("/api/auth/dummy-endpoint");
        exceededResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Authorization_RequirePremiumRole_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();

        // RateLimiter (StrictLimiter) testinden kalan kotaların sıfırlanması için 1 saniye bekliyoruz
        // (WebApplicationFactory aynı instance'ı kullandığı ve IP "anonymous" olduğu için testler çakışabilir)
        await Task.Delay(1100);

        var response = await client.GetAsync("/api/shuffle/vip-weekend");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
