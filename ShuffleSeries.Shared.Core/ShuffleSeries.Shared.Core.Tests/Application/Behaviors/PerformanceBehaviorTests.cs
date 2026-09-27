using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ShuffleSeries.Shared.Core.Application.Behaviors;

#pragma warning disable CA1873 // Avoid unevaluated expensive arguments in test assertions

namespace ShuffleSeries.Shared.Core.Tests.Application.Behaviors;

public class PerformanceBehaviorTests
{
    public record FastCommand(string Name) : IRequest<string>;

    private readonly Mock<ILogger<PerformanceBehavior<FastCommand, string>>> _mockLogger = new();

    public PerformanceBehaviorTests()
    {
        _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public async Task Handle_WhenRequestIsFast_ShouldNotLogWarning()
    {
        // Arrange
        var behavior = new PerformanceBehavior<FastCommand, string>(_mockLogger.Object);
        var command = new FastCommand("Fast");

        RequestHandlerDelegate<string> next = (cancellationToken) => Task.FromResult("OK");

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be("OK");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Long Running Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
#pragma warning restore CA1873
