using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Events;

namespace ShuffleSeries.Catalog.Tests.Domain.Entities;

public class PlatformTests
{
    [Fact]
    public void Create_Should_CreatePlatformAndRaiseEvent()
    {
        // Act
        var platform = Platform.Create(
            name: "Netflix",
            slug: "netflix",
            logoUrl: "https://example.com/netflix.svg",
            inactiveLogoUrl: "https://example.com/netflix-inactive.svg",
            baseWebUrl: "https://netflix.com",
            baseAppSchemeUrl: "netflix://",
            displayOrder: 1);

        // Assert
        platform.Should().NotBeNull();
        platform.Id.Should().NotBeEmpty();
        platform.Name.Should().Be("Netflix");
        platform.Slug.Should().Be("netflix");
        platform.LogoUrl.Should().Be("https://example.com/netflix.svg");
        platform.InactiveLogoUrl.Should().Be("https://example.com/netflix-inactive.svg");
        platform.BaseWebUrl.Should().Be("https://netflix.com");
        platform.BaseAppSchemeUrl.Should().Be("netflix://");
        platform.DisplayOrder.Should().Be(1);
        platform.IsActive.Should().BeTrue();

        platform.GetDomainEvents().Should().ContainSingle(e => e is PlatformCreatedDomainEvent);
    }

    [Fact]
    public void Update_Should_UpdateDetailsAndRaiseEvent()
    {
        // Arrange
        var platform = Platform.Create("Netflix", "netflix", "logo1", "inact1", "https://netflix.com", "netflix://", 1);
        platform.ClearDomainEvents();

        // Act
        platform.Update("Netflix Premium", "netflix-prem", "logo2", "inact2", "https://netflix.com/tr", "nflx://", 2);

        // Assert
        platform.Name.Should().Be("Netflix Premium");
        platform.Slug.Should().Be("netflix-prem");
        platform.LogoUrl.Should().Be("logo2");
        platform.InactiveLogoUrl.Should().Be("inact2");
        platform.BaseWebUrl.Should().Be("https://netflix.com/tr");
        platform.BaseAppSchemeUrl.Should().Be("nflx://");
        platform.DisplayOrder.Should().Be(2);
        platform.ModifiedAtUtc.Should().NotBeNull();

        platform.GetDomainEvents().Should().ContainSingle(e => e is PlatformUpdatedDomainEvent);
    }

    [Theory]
    [InlineData("", "slug")]
    [InlineData("Name", "")]
    public void Create_ShouldThrowArgumentException_WhenRequiredFieldIsEmpty(string name, string slug)
    {
        var action = () => Platform.Create(name, slug, "logo", "inact", "https://web.com", "scheme://", 1);
        action.Should().Throw<ArgumentException>();
    }
}
