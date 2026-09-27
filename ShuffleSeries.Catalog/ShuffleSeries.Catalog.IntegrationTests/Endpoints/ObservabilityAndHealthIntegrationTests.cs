using System.Net;
using System.Text.Json;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Web.Correlation;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class ObservabilityAndHealthIntegrationTests : CatalogIntegrationTestBase
{
    public ObservabilityAndHealthIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LivenessProbe_ShouldReturn200Ok_WithLivenessPayload()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("status").GetString().Should().Be("Healthy");
        root.GetProperty("type").GetString().Should().Be("Liveness");
    }

    [Fact]
    public async Task ReadinessProbe_ShouldReturn200Ok_WithReadinessPayload()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("status").GetString().Should().Be("Healthy");
        root.GetProperty("type").GetString().Should().Be("Readiness");
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_ShouldGenerateAndReturnCorrelationIdInResponse()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        response.Headers.Contains(CorrelationIdConstants.HeaderName).Should().BeTrue();
        var correlationId = response.Headers.GetValues(CorrelationIdConstants.HeaderName).FirstOrDefault();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Request_WithCustomCorrelationIdHeader_ShouldEchoSameCorrelationIdInResponse()
    {
        // Arrange
        var customCorrelationId = "custom-test-correlation-id-99999";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdConstants.HeaderName, customCorrelationId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.Headers.Contains(CorrelationIdConstants.HeaderName).Should().BeTrue();
        var returnedId = response.Headers.GetValues(CorrelationIdConstants.HeaderName).FirstOrDefault();
        returnedId.Should().Be(customCorrelationId);
    }

    [Fact]
    public async Task Request_WithMalformedCrlfCorrelationIdHeader_ShouldRejectAndEchoSanitizedGuid()
    {
        // Arrange
        var maliciousHeader = "evil-id\r\nX-Injected: attack";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.TryAddWithoutValidation(CorrelationIdConstants.HeaderName, maliciousHeader);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.Headers.Contains(CorrelationIdConstants.HeaderName).Should().BeTrue();
        var returnedId = response.Headers.GetValues(CorrelationIdConstants.HeaderName).FirstOrDefault();
        returnedId.Should().NotBeNullOrWhiteSpace();
        returnedId.Should().NotContain("\r");
        returnedId.Should().NotContain("\n");
        Guid.TryParse(returnedId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task NotFoundRequest_ShouldReturnProblemDetails_ContainingCorrelationIdAndTraceId()
    {
        // Arrange
        var customCorrelationId = "corr-error-trace-777";
        var nonExistentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/catalog/series/{nonExistentId}");
        request.Headers.Add(CorrelationIdConstants.HeaderName, customCorrelationId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Contains(CorrelationIdConstants.HeaderName).Should().BeTrue();
        response.Headers.GetValues(CorrelationIdConstants.HeaderName).First().Should().Be(customCorrelationId);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("traceId", out var traceIdProp).Should().BeTrue();
        traceIdProp.GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("correlationId", out var correlationIdProp).Should().BeTrue();
        correlationIdProp.GetString().Should().Be(customCorrelationId);
    }
}
