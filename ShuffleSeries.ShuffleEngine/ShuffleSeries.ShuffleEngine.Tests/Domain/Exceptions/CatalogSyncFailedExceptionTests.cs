using System.Net;
using ShuffleSeries.ShuffleEngine.Domain.Exceptions;

namespace ShuffleSeries.ShuffleEngine.Tests.Domain.Exceptions;

public class CatalogSyncFailedExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetServiceUnavailableStatusAndErrorCode()
    {
        var ex = new CatalogSyncFailedException("Connection refused");

        ex.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        ex.Code.Should().Be("CATALOG_SYNC_FAILED");
        ex.Message.Should().Contain("Connection refused");
    }
}
