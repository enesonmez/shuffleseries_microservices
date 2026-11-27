using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
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
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.10");

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/api/auth/dummy-endpoint");
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var exceededResponse = await client.GetAsync("/api/auth/dummy-endpoint");
        exceededResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task CorrelationId_ShouldBePreserved_WhenHeaderProvided()
    {
        var client = _factory.CreateClient();
        const string expectedCorrelationId = "custom-test-correlation-id-98765";
        client.DefaultRequestHeaders.Add("X-Correlation-ID", expectedCorrelationId);

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values.Should().Contain(expectedCorrelationId);
    }

    [Fact]
    public async Task CorrelationId_ShouldBeGenerated_WhenHeaderMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        var generatedId = values!.FirstOrDefault();
        generatedId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(generatedId, out _).Should().BeTrue("Otomatik üretilen CorrelationId geçerli bir GUID olmalıdır.");
    }

    [Fact]
    public async Task HealthProbes_LivenessAndReadiness_ShouldReturn200AndValidJson()
    {
        var client = _factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var liveContent = await liveResponse.Content.ReadAsStringAsync();
        liveContent.Should().Contain("Healthy");
        liveContent.Should().Contain("Liveness");

        var readyResponse = await client.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var readyContent = await readyResponse.Content.ReadAsStringAsync();
        readyContent.Should().Contain("Readiness");
    }

    [Fact]
    public async Task SwaggerUI_ShouldReturn200OK()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task DownstreamUnreachable_ShouldReturnProblemDetails_WithDownstreamServiceUnavailableCode()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.30");

        // Catalog cluster downstream'i yerel birimde kapalı olduğu için proxy error handler devreye girer
        var response = await client.GetAsync("/catalog-api/openapi/unreachable-test");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("code");
        problem.Extensions["code"]?.ToString().Should().Be("DOWNSTREAM_SERVICE_UNAVAILABLE");
    }

    [Fact]
    public async Task CatalogRoute_WithoutAuthToken_ShouldReturn401Unauthorized_AtGatewayEdge()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.40");

        var response = await client.GetAsync("/api/catalog/movies");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("code");
        problem.Extensions["code"]?.ToString().Should().Be("AUTH_UNAUTHORIZED");
    }

    [Fact]
    public async Task ShuffleRoute_WithoutAuthToken_ShouldReturn401Unauthorized_AtGatewayEdge()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.45");

        var response = await client.GetAsync("/api/shuffle/admin/pool-status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("code");
        problem.Extensions["code"]?.ToString().Should().Be("AUTH_UNAUTHORIZED");
    }

    [Theory]
    [InlineData("/catalog-api/swagger/index.html")]
    [InlineData("/catalog-api/scalar/v1")]
    [InlineData("/identity-api/swagger/index.html")]
    [InlineData("/identity-api/scalar/v1")]
    [InlineData("/shuffle-api/swagger/index.html")]
    [InlineData("/shuffle-api/scalar/v1")]
    [InlineData("/api/search/anything")]
    public async Task RemovedSubServiceUiRoutes_ShouldReturn404NotFound(string removedPath)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.50");

        var response = await client.GetAsync(removedPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
