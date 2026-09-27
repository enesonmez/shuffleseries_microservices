using System.Net;
using Microsoft.AspNetCore.Http;
using ShuffleSeries.Shared.Core.Web.Correlation;

namespace ShuffleSeries.Shared.Core.Tests.Web.Correlation;

public class CorrelationIdTests
{
    [Fact]
    public void CorrelationIdContext_WhenInitialized_ShouldHaveNonEmptyCorrelationId()
    {
        // Arrange & Act
        var context = new CorrelationIdContext();

        // Assert
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(context.CorrelationId, out _).Should().BeTrue();
    }

    [Fact]
    public void CorrelationIdContext_SetCorrelationId_ShouldUpdateValue()
    {
        // Arrange
        var context = new CorrelationIdContext();
        var customId = "custom-trace-12345";

        // Act
        context.SetCorrelationId(customId);

        // Assert
        context.CorrelationId.Should().Be(customId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderProvided_ShouldUseProvidedHeader()
    {
        // Arrange
        var correlationContext = new CorrelationIdContext();
        var httpContext = new DefaultHttpContext();
        var customCorrelationId = "incoming-correlation-abc-123";
        httpContext.Request.Headers[CorrelationIdConstants.HeaderName] = customCorrelationId;

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext, correlationContext);

        // Assert
        nextCalled.Should().BeTrue();
        correlationContext.CorrelationId.Should().Be(customCorrelationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderMissing_ShouldGenerateNewCorrelationId()
    {
        // Arrange
        var correlationContext = new CorrelationIdContext();
        var httpContext = new DefaultHttpContext();

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext, correlationContext);

        // Assert
        nextCalled.Should().BeTrue();
        correlationContext.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderContainsCrlfInjection_ShouldRejectAndGenerateSafeGuid()
    {
        // Arrange
        var correlationContext = new CorrelationIdContext();
        var httpContext = new DefaultHttpContext();
        var maliciousHeader = "injected-id\r\nX-Injected-Header: evil";
        httpContext.Request.Headers[CorrelationIdConstants.HeaderName] = maliciousHeader;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, correlationContext);

        // Assert
        correlationContext.CorrelationId.Should().NotBe(maliciousHeader);
        correlationContext.CorrelationId.Should().NotContain("\r");
        correlationContext.CorrelationId.Should().NotContain("\n");
        Guid.TryParse(correlationContext.CorrelationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenHeaderExceedsMaxLength_ShouldRejectAndGenerateSafeGuid()
    {
        // Arrange
        var correlationContext = new CorrelationIdContext();
        var httpContext = new DefaultHttpContext();
        var excessivelyLongHeader = new string('A', 200);
        httpContext.Request.Headers[CorrelationIdConstants.HeaderName] = excessivelyLongHeader;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, correlationContext);

        // Assert
        correlationContext.CorrelationId.Should().NotBe(excessivelyLongHeader);
        Guid.TryParse(correlationContext.CorrelationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationIdDelegatingHandler_ShouldAppendHeaderToOutgoingRequest()
    {
        // Arrange
        var correlationContext = new CorrelationIdContext();
        correlationContext.SetCorrelationId("outgoing-corr-999");

        var testHandler = new TestHttpMessageHandler();
        var delegatingHandler = new CorrelationIdDelegatingHandler(correlationContext)
        {
            InnerHandler = testHandler
        };

        var client = new HttpClient(delegatingHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.internal.service/test");

        // Act
        await client.SendAsync(request);

        // Assert
        testHandler.LastRequest.Should().NotBeNull();
        testHandler.LastRequest!.Headers.Contains(CorrelationIdConstants.HeaderName).Should().BeTrue();
        testHandler.LastRequest.Headers.GetValues(CorrelationIdConstants.HeaderName).First().Should().Be("outgoing-corr-999");
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
