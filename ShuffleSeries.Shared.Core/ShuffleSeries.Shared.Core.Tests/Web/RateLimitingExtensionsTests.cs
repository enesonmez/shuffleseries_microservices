using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Web.RateLimiting;

namespace ShuffleSeries.Shared.Core.Tests.Web;

public class RateLimitingExtensionsTests
{
    [Fact]
    public void AddSharedRateLimiter_ShouldRegisterRateLimiterOptionsAndPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        // Null logger ve diğer servis bağımlılıklarını simüle edebiliriz ancak AddRateLimiter doğrudan IServiceCollection kullanır

        // Act
        services.AddSharedRateLimiter();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        options.GlobalLimiter.Should().NotBeNull();

        // Sınırlı olsa da poliçenin kaydedildiğini (policy adıyla çağrılarak hata fırlatılmadığını) dolaylı kontrol edebiliriz
        // (AspNetCore.RateLimiting içindeki policyleri reflection olmadan doğrudan listelemek mümkün değil)
    }
}
