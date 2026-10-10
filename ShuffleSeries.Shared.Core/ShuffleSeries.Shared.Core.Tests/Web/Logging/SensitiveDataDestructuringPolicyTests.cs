using Serilog.Core;
using Serilog.Events;
using ShuffleSeries.Shared.Core.Domain.Attributes;
using ShuffleSeries.Shared.Core.Web.Logging;

namespace ShuffleSeries.Shared.Core.Tests.Web.Logging;

public class SensitiveDataDestructuringPolicyTests
{
    private readonly SensitiveDataDestructuringPolicy _policy = new();
    private readonly TestPropertyValueFactory _factory = new();

    private sealed class TestPropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
        {
            if (value is null)
                return new ScalarValue(null);

            var policy = new SensitiveDataDestructuringPolicy();
            if (destructureObjects && policy.TryDestructure(value, this, out var destructured))
            {
                return destructured;
            }

            return new ScalarValue(value);
        }
    }

    public record UserRegistrationCommand(
        string Username,
        string Password,
        [property: MaskSensitiveData("###HIDDEN###")] string CustomSecret,
        string Email,
        string AuthToken,
        string CvvNumber,
        int Age
    );

    public record ComplexOrderCommand(
        string OrderId,
        CreditCardInfo Card,
        string CustomerEmail
    );

    public record CreditCardInfo(
        string CardNumber,
        string Cvv,
        string Expiry
    );

    [Fact]
    public void TryDestructure_WhenValueIsNull_ShouldReturnFalse()
    {
        var result = _policy.TryDestructure(null!, _factory, out var propertyValue);

        result.Should().BeFalse();
        propertyValue.Should().BeNull();
    }

    [Theory]
    [InlineData("hello")]
    [InlineData(123)]
    [InlineData(true)]
    [InlineData(12.34)]
    public void TryDestructure_WhenValueIsPrimitiveOrScalar_ShouldReturnFalse(object scalar)
    {
        var result = _policy.TryDestructure(scalar, _factory, out var propertyValue);

        result.Should().BeFalse();
        propertyValue.Should().BeNull();
    }

    [Fact]
    public void TryDestructure_WhenValueIsDateTimeOrGuid_ShouldReturnFalse()
    {
        _policy.TryDestructure(DateTime.UtcNow, _factory, out var pv1).Should().BeFalse();
        _policy.TryDestructure(Guid.NewGuid(), _factory, out var pv2).Should().BeFalse();
    }

    [Fact]
    public void TryDestructure_WhenValueIsList_ShouldReturnFalse()
    {
        var list = new List<string> { "item1", "item2" };
        var result = _policy.TryDestructure(list, _factory, out var propertyValue);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryDestructure_WhenObjectHasSensitiveData_ShouldMaskProperly()
    {
        // Arrange
        var command = new UserRegistrationCommand(
            Username: "enes",
            Password: "SuperSecretPassword123!",
            CustomSecret: "my_api_key_xyz",
            Email: "enes@shuffleseries.com",
            AuthToken: "jwt-token-val",
            CvvNumber: "999",
            Age: 30);

        // Act
        var success = _policy.TryDestructure(command, _factory, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().BeOfType<StructureValue>();

        var structure = (StructureValue)result!;
        var properties = structure.Properties.ToDictionary(p => p.Name, p => p.Value);

        // Sensitive properties masked
        properties["Password"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***MASKED***");
        properties["CustomSecret"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("###HIDDEN###");
        properties["AuthToken"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***MASKED***");
        properties["CvvNumber"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***MASKED***");

        // Non-sensitive properties preserved
        properties["Username"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("enes");
        properties["Email"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("enes@shuffleseries.com");
        properties["Age"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be(30);
    }

    [Fact]
    public void TryDestructure_WhenObjectHasNestedObjects_ShouldMaskRecursively()
    {
        // Arrange
        var card = new CreditCardInfo("1234-5678-9012-3456", "123", "12/28");
        var order = new ComplexOrderCommand("ORD-100", card, "customer@example.com");

        // Act
        var success = _policy.TryDestructure(order, _factory, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().BeOfType<StructureValue>();

        var structure = (StructureValue)result!;
        var properties = structure.Properties.ToDictionary(p => p.Name, p => p.Value);

        properties["OrderId"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("ORD-100");
        properties["CustomerEmail"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("customer@example.com");

        // Nested card
        var cardStructure = properties["Card"].Should().BeOfType<StructureValue>().Subject;
        var cardProps = cardStructure.Properties.ToDictionary(p => p.Name, p => p.Value);

        cardProps["CardNumber"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***MASKED***");
        cardProps["Cvv"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***MASKED***");
        cardProps["Expiry"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("12/28");
    }

    private sealed class FaultyPropertyClass
    {
        private readonly string _state = "initialized";

        public string NormalProperty => _state;

        public string FailingProperty
        {
            get
            {
                if (_state.Length > 0)
                {
                    throw new InvalidOperationException("Property read failed");
                }

                return string.Empty;
            }
        }
    }

    [Fact]
    public void TryDestructure_WhenPropertyThrowsExceptionOnGet_ShouldCaptureErrorPlaceholder()
    {
        // Arrange
        var target = new FaultyPropertyClass();

        // Act
        var success = _policy.TryDestructure(target, _factory, out var result);

        // Assert
        success.Should().BeTrue();
        var structure = result.Should().BeOfType<StructureValue>().Subject;
        var properties = structure.Properties.ToDictionary(p => p.Name, p => p.Value);

        properties["NormalProperty"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("initialized");
        properties["FailingProperty"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("<error reading property>");
    }

    [Fact]
    public void TryDestructure_WhenValueIsUriOrTimeSpanOrDateTimeOffset_ShouldReturnFalse()
    {
        _policy.TryDestructure(new Uri("https://example.com"), _factory, out var pv1).Should().BeFalse();
        _policy.TryDestructure(TimeSpan.FromMinutes(5), _factory, out var pv2).Should().BeFalse();
        _policy.TryDestructure(DateTimeOffset.UtcNow, _factory, out var pv3).Should().BeFalse();
    }

    [Fact]
    public void TryDestructure_WhenValueIsCancellationTokenOrStream_ShouldReturnFalse()
    {
        _policy.TryDestructure(CancellationToken.None, _factory, out var pv1).Should().BeFalse();
        using var stream = new MemoryStream();
        _policy.TryDestructure(stream, _factory, out var pv2).Should().BeFalse();
    }
}
