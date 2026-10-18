using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Shared.Core.Tests.Domain.ValueObjects;

public class LanguageCodeTests
{
    [Theory]
    [InlineData("tr", "tr", "Türkçe", "Turkish")]
    [InlineData("TR", "tr", "Türkçe", "Turkish")]
    [InlineData(" tr ", "tr", "Türkçe", "Turkish")]
    [InlineData("en", "en", "English", "English")]
    [InlineData("EN", "en", "English", "English")]
    [InlineData("de", "de", "Deutsch", "German")]
    [InlineData("es", "es", "Español", "Spanish")]
    [InlineData("fr", "fr", "Français", "French")]
    public void From_WithValidSupportedCode_ShouldReturnExpectedInstance(
        string input,
        string expectedValue,
        string expectedNativeName,
        string expectedEnglishName)
    {
        // Act
        var result = LanguageCode.From(input);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(expectedValue);
        result.NativeName.Should().Be(expectedNativeName);
        result.EnglishName.Should().Be(expectedEnglishName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhitespace_ShouldThrowArgumentException(string? input)
    {
        // Act
        var act = () => LanguageCode.From(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null, empty, or whitespace*");
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("invalid")]
    [InlineData("tur")]
    [InlineData("123")]
    public void From_WithUnsupportedCode_ShouldThrowArgumentException(string input)
    {
        // Act
        var act = () => LanguageCode.From(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*Language code '{input}' is not supported*");
    }

    [Theory]
    [InlineData("tr", true)]
    [InlineData("EN", true)]
    [InlineData("de", true)]
    [InlineData("es", true)]
    [InlineData("fr", true)]
    [InlineData("zz", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryFrom_Should_ReturnExpectedResult(string? input, bool expectedSuccess)
    {
        // Act
        var success = LanguageCode.TryFrom(input, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Should().NotBeNull();
            result!.Value.Should().Be(input!.Trim().ToLowerInvariant());
        }
        else
        {
            result.Should().BeNull();
        }
    }

    [Theory]
    [InlineData("tr", true)]
    [InlineData("TR", true)]
    [InlineData("en", true)]
    [InlineData("de", true)]
    [InlineData("es", true)]
    [InlineData("fr", true)]
    [InlineData("xx", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSupported_Should_ReturnExpectedBoolean(string? input, bool expected)
    {
        // Act
        var result = LanguageCode.IsSupported(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ImplicitOperator_ToString_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var lang = LanguageCode.Turkish;

        // Act
        string str = lang;

        // Assert
        str.Should().Be("tr");
    }

    [Fact]
    public void ExplicitOperator_FromString_ShouldParseValidCode()
    {
        // Act
        var lang = (LanguageCode)"en";

        // Assert
        lang.Should().Be(LanguageCode.English);
    }

    [Fact]
    public void EqualityAndComparison_ShouldWorkAsExpectedForRecord()
    {
        // Arrange
        var tr1 = LanguageCode.From("tr");
        var tr2 = LanguageCode.Turkish;
        var en = LanguageCode.English;

        // Assert
        tr1.Should().Be(tr2);
        (tr1 == tr2).Should().BeTrue();
        (tr1 != en).Should().BeTrue();
        tr1.GetHashCode().Should().Be(tr2.GetHashCode());
        tr1.ToString().Should().Be("tr");

        tr1.CompareTo(en).Should().BeGreaterThan(0); // "tr" > "en"
    }

    [Fact]
    public void SupportedCollection_ShouldContainAllPredefinedLanguages()
    {
        // Assert
        LanguageCode.Supported.Should().HaveCount(5);
        LanguageCode.SupportedValues.Should().Contain(new[] { "tr", "en", "de", "es", "fr" });
    }
}
