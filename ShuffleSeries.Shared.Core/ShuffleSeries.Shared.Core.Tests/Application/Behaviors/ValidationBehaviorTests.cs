using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using ShuffleSeries.Shared.Core.Application.Behaviors;
using ValidationException = ShuffleSeries.Shared.Core.Exceptions.ValidationException;

namespace ShuffleSeries.Shared.Core.Tests.Application.Behaviors;

public class ValidationBehaviorTests
{
    public record TestCommand(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenNoValidatorsConfigured_ShouldCallNextDelegate()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Test");
        var nextCalled = false;

        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_WhenValidatorsPass_ShouldCallNextDelegate()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestCommand>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { mockValidator.Object });
        var request = new TestCommand("Valid");
        var nextCalled = false;

        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationExceptionWithErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required."),
            new("Name", "Name cannot be empty.")
        };

        var mockValidator = new Mock<IValidator<TestCommand>>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestCommand, string>(new[] { mockValidator.Object });
        var request = new TestCommand("");

        RequestHandlerDelegate<string> next = (cancellationToken) => Task.FromResult("Success");

        // Act
        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        ex.Which.Errors["Name"].Should().Contain("Name is required.");
    }
}
