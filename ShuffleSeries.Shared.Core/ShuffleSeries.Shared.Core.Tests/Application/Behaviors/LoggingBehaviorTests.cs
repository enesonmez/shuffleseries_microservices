using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ShuffleSeries.Shared.Core.Application.Behaviors;

#pragma warning disable CA1873 // Avoid unevaluated expensive arguments in test assertions

namespace ShuffleSeries.Shared.Core.Tests.Application.Behaviors;

public class LoggingBehaviorTests
{
    public record SampleAuthCommand(string Username, string Password) : IRequest<SampleAuthResponse>;
    public record SampleAuthResponse(string Token, string Status);

    private readonly Mock<ILogger<LoggingBehavior<SampleAuthCommand, SampleAuthResponse>>> _mockLogger = new();

    public LoggingBehaviorTests()
    {
        _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public async Task Handle_WhenRequestSucceeds_ShouldEmitSingleConsolidatedLog()
    {
        // Arrange
        var behavior = new LoggingBehavior<SampleAuthCommand, SampleAuthResponse>(_mockLogger.Object);
        var command = new SampleAuthCommand("enes", "secret123");
        var expectedResponse = new SampleAuthResponse("token_xyz", "Success");

        var nextCalled = false;
        RequestHandlerDelegate<SampleAuthResponse> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var response = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        response.Should().Be(expectedResponse);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled request SampleAuthCommand")
                                             && v.ToString()!.Contains("Request:")
                                             && v.ToString()!.Contains("Response:")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_ShouldLogWarningWithRequestPayloadAndRethrowWithoutDuplicateErrorLogging()
    {
        // Arrange
        var behavior = new LoggingBehavior<SampleAuthCommand, SampleAuthResponse>(_mockLogger.Object);
        var command = new SampleAuthCommand("enes", "secret123");

        var expectedException = new InvalidOperationException("Database down");
        RequestHandlerDelegate<SampleAuthResponse> next = (cancellationToken) => throw expectedException;

        // Act
        Func<Task> act = async () => await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Be("Database down");

        // The failed request must log its payload as a warning so troubleshooting is possible
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request SampleAuthCommand failed")
                                             && v.ToString()!.Contains("Database down")
                                             && v.ToString()!.Contains("Request:")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Success log should not be emitted
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        // Error logging and stack trace are delegated exclusively to GlobalExceptionHandler (no S2139 violation)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
#pragma warning restore CA1873
