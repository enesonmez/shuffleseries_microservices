using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using ShuffleSeries.Shared.Core.Application.Common.Interfaces;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;
using ShuffleSeries.Shared.Core.Web.Localization;

namespace ShuffleSeries.Shared.Core.Tests.Web.Localization;

public class LanguageContextTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public LanguageContextTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    [Fact]
    public void CurrentLanguage_Should_ReturnDefaultTurkish_When_HttpContextIsNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var context = new LanguageContext(_httpContextAccessorMock.Object);

        // Act
        var result = context.CurrentLanguage;

        // Assert
        result.Should().Be(LanguageCode.Turkish.Value);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("tr", "tr")]
    [InlineData("EN", "en")]
    public void CurrentLanguage_Should_ResolveFromQueryParameter_When_Supported(string queryValue, string expected)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "lang", new StringValues(queryValue) }
        });

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var context = new LanguageContext(_httpContextAccessorMock.Object);

        // Act
        var result = context.CurrentLanguage;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CurrentLanguage_Should_FallbackToAcceptLanguage_When_QueryParamIsUnsupported()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "lang", new StringValues("unsupported-lang") }
        });
        httpContext.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var context = new LanguageContext(_httpContextAccessorMock.Object);

        // Act
        var result = context.CurrentLanguage;

        // Assert
        result.Should().Be("en");
    }

    [Theory]
    [InlineData("en-US,en;q=0.9,tr;q=0.8", "en")]
    [InlineData("tr-TR,tr;q=0.9", "tr")]
    [InlineData("ja-JP,ja;q=0.9,en;q=0.8", "en")] // ja is unsupported, skips to en
    public void CurrentLanguage_Should_ResolveFromAcceptLanguageHeader(string headerValue, string expected)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.AcceptLanguage = headerValue;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var context = new LanguageContext(_httpContextAccessorMock.Object);

        // Act
        var result = context.CurrentLanguage;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CurrentLanguage_Should_FallbackToTurkish_When_NoSupportedLanguageFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.AcceptLanguage = "ja-JP,zh-CN;q=0.9";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var context = new LanguageContext(_httpContextAccessorMock.Object);

        // Act
        var result = context.CurrentLanguage;

        // Assert
        result.Should().Be(LanguageCode.Turkish.Value);
    }

    [Fact]
    public void CurrentLanguage_Should_CacheResolvedValue_AcrossMultipleReads()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "lang", new StringValues("en") }
        });

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var context = new LanguageContext(_httpContextAccessorMock.Object);

        // Act
        var firstRead = context.CurrentLanguage;
        // Mutate query parameter after first read
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "lang", new StringValues("tr") }
        });
        var secondRead = context.CurrentLanguage;

        // Assert: Cached value "en" is retained
        firstRead.Should().Be("en");
        secondRead.Should().Be("en");
    }

    [Fact]
    public void AddSharedLocalization_Should_RegisterServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSharedLocalization();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
        provider.GetService<ILanguageContext>().Should().NotBeNull();
        provider.GetService<ILanguageContext>().Should().BeOfType<LanguageContext>();
    }
}
