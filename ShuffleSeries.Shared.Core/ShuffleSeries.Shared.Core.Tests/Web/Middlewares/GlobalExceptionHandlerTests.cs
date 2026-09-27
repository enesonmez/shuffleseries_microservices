using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ShuffleSeries.Shared.Core.Exceptions;
using ShuffleSeries.Shared.Core.Web.Middlewares;

namespace ShuffleSeries.Shared.Core.Tests.Web.Middlewares;

public class GlobalExceptionHandlerTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock = new();
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    private static (DefaultHttpContext context, MemoryStream stream) CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var stream = new MemoryStream();
        context.Response.Body = stream;
        context.Request.Path = "/api/test-endpoint";
        return (context, stream);
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(MemoryStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, _jsonOptions);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_ShouldReturn400WithErrors()
    {
        var (context, stream) = CreateHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required"]
        };
        var ex = new ValidationException(errors);

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().StartWith("application/problem+json");

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Validation Error");
        problemDetails.Instance.Should().Be("/api/test-endpoint");
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_ShouldReturn404()
    {
        var (context, stream) = CreateHttpContext();
        var ex = new NotFoundException("Series", Guid.NewGuid());

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Extensions.Should().ContainKey("code");
        problemDetails.Extensions["code"]?.ToString().Should().Be("SERIES_NOT_FOUND");
    }

    [Fact]
    public async Task TryHandleAsync_BusinessException_ShouldReturn422()
    {
        var (context, stream) = CreateHttpContext();
        var ex = new BusinessException("SERIES_EXISTS", "A series with this title already exists.");

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problemDetails.Title.Should().Be("Business Rule Violation");
        problemDetails.Detail.Should().Be("A series with this title already exists.");
        problemDetails.Extensions["code"]?.ToString().Should().Be("SERIES_EXISTS");
    }

    [Fact]
    public async Task TryHandleAsync_ConflictException_ShouldReturn409()
    {
        var (context, stream) = CreateHttpContext();
        var ex = new ConflictException("Conflict detected");

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status409Conflict);
        problemDetails.Title.Should().Be("Conflict");
    }

    [Fact]
    public async Task TryHandleAsync_ArgumentException_ShouldReturn400()
    {
        var (context, stream) = CreateHttpContext();
        var ex = new ArgumentException("Title cannot be empty");

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Be("Title cannot be empty");
    }

    [Fact]
    public async Task TryHandleAsync_BadRequestException_ShouldReturn400WithCode()
    {
        var (context, stream) = CreateHttpContext();
        var ex = new BadRequestException("Route ID and Command ID must match.", "ID_MISMATCH");

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Be("Route ID and Command ID must match.");
        problemDetails.Extensions.Should().ContainKey("code");
        problemDetails.Extensions["code"]?.ToString().Should().Be("ID_MISMATCH");
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledException_ShouldReturn500WithGenericMessage()
    {
        var (context, stream) = CreateHttpContext();
        var ex = new InvalidOperationException("Sensitive internal database connection string error");

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Title.Should().Be("Internal Server Error");
        problemDetails.Detail.Should().Be("An unexpected error occurred on the server.");
        problemDetails.Detail.Should().NotContain("Sensitive");
    }

    [Fact]
    public async Task TryHandleAsync_WhenResponseHasStarted_ShouldReturnFalse()
    {
        var httpResponseMock = new Mock<HttpResponse>();
        httpResponseMock.Setup(r => r.HasStarted).Returns(true);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);

        var result = await _handler.TryHandleAsync(httpContextMock.Object, new Exception("Test"), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void AddSharedExceptionHandling_ShouldRegisterServices()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ShuffleSeries.Shared.Core.Web.DependencyInjection.AddSharedExceptionHandling(services);

        services.Should().Contain(d => d.ImplementationType == typeof(GlobalExceptionHandler));
    }

    [Fact]
    public void UseSharedExceptionHandling_ShouldConfigureMiddleware()
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        ShuffleSeries.Shared.Core.Web.DependencyInjection.AddSharedExceptionHandling(builder.Services);
        var app = builder.Build();

        var result = ShuffleSeries.Shared.Core.Web.DependencyInjection.UseSharedExceptionHandling(app);

        result.Should().NotBeNull();
    }

    private sealed class UntypedCustomException : CustomException
    {
        public UntypedCustomException(string message, System.Net.HttpStatusCode statusCode)
            : base(message, statusCode, code: null, title: null) { }
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Unauthorized, "Unauthorized", "https://tools.ietf.org/html/rfc7235#section-3.1")]
    [InlineData(System.Net.HttpStatusCode.Forbidden, "Forbidden", "https://tools.ietf.org/html/rfc7231#section-6.5.3")]
    [InlineData(System.Net.HttpStatusCode.BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc7231#section-6.5.1")]
    [InlineData(System.Net.HttpStatusCode.NotFound, "Not Found", "https://tools.ietf.org/html/rfc7231#section-6.5.4")]
    [InlineData(System.Net.HttpStatusCode.Conflict, "Conflict", "https://tools.ietf.org/html/rfc7231#section-6.5.8")]
    [InlineData(System.Net.HttpStatusCode.UnprocessableEntity, "Business Rule Violation", "https://tools.ietf.org/html/rfc4918#section-11.2")]
    [InlineData(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1")]
    public async Task TryHandleAsync_CustomExceptionWithNullTitle_ShouldFallBackToDefaultTitleAndRfc(
        System.Net.HttpStatusCode statusCode, string expectedTitle, string expectedType)
    {
        var (context, stream) = CreateHttpContext();
        var ex = new UntypedCustomException("Error without explicit title", statusCode);

        var result = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)statusCode);

        var problemDetails = await ReadProblemDetailsAsync(stream);
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be(expectedTitle);
        problemDetails.Type.Should().Be(expectedType);
        problemDetails.Detail.Should().Be("Error without explicit title");
    }
}
