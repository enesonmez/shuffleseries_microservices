using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Web.Authorization;
using ShuffleSeries.Shared.Core.Web.Correlation;

namespace ShuffleSeries.Shared.Core.Tests.Web.Authorization;

public class ProblemDetailsAuthorizationMiddlewareResultHandlerTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ProblemDetailsAuthorizationMiddlewareResultHandler _sut = new();

    [Fact]
    public async Task HandleAsync_WhenForbidden_ShouldReturn403ProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var services = new ServiceCollection();
        var correlationMock = new CorrelationIdContext();
        correlationMock.SetCorrelationId("test-correlation-123");
        services.AddSingleton<ICorrelationIdContext>(correlationMock);
        context.RequestServices = services.BuildServiceProvider();
        context.Request.Path = "/api/v1/secure-data";

        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();
        var authorizeResult = PolicyAuthorizationResult.Forbid();

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _sut.HandleAsync(next, context, policy, authorizeResult);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        context.Response.ContentType.Should().Contain("application/problem+json");

        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody);
        var json = await reader.ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(json, _jsonOptions);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status403Forbidden);
        problemDetails.Title.Should().Be("Forbidden");
        problemDetails.Detail.Should().Contain("yetki");
        problemDetails.Instance.Should().Be("/api/v1/secure-data");
        problemDetails.Extensions.Should().ContainKey("code");
        problemDetails.Extensions["code"]?.ToString().Should().Be("AUTH_FORBIDDEN");
        problemDetails.Extensions.Should().ContainKey("correlationId");
        problemDetails.Extensions["correlationId"]?.ToString().Should().Be("test-correlation-123");
    }

    [Fact]
    public async Task HandleAsync_WhenSuccess_ShouldCallNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        var authorizeResult = PolicyAuthorizationResult.Success();

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        // Act
        await _sut.HandleAsync(next, context, policy, authorizeResult);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
