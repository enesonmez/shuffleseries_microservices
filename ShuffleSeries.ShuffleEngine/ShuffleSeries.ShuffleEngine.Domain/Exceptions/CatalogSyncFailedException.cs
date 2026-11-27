using System.Net;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.ShuffleEngine.Domain.Exceptions;

public sealed class CatalogSyncFailedException : CustomException
{
    public CatalogSyncFailedException(string reason)
        : base(
            $"Catalog synchronization via gRPC failed: {reason}",
            HttpStatusCode.ServiceUnavailable,
            "CATALOG_SYNC_FAILED")
    {
    }
}
