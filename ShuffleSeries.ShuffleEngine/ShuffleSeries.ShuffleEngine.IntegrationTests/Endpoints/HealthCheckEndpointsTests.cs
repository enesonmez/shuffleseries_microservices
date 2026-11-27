using System.Net;
using ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Endpoints;

public class HealthCheckEndpointsTests(ShuffleEngineApiFactory factory) : ShuffleEngineIntegrationTestBase(factory)
{
    [Fact]
    public async Task Get_HealthLive_And_HealthReady_ShouldReturnOk()
    {
        using var anonClient = CreateUnauthorizedClient();

        var liveResponse = await anonClient.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var readyResponse = await anonClient.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
