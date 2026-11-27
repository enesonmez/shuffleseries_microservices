using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.ShuffleEngine.Api.Contracts.Requests.CacheSync;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Commands.WarmShuffleCache;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Queries.GetShufflePoolStatus;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;
using Wolverine;

namespace ShuffleSeries.ShuffleEngine.Api.Endpoints;

public static class ShuffleAdminEndpoints
{
    public static void MapShuffleAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/shuffle/admin")
            .WithTags("Shuffle Admin & Sync")
            .RequireAuthorization(SystemPolicies.RequireAdminRole);

        group.MapGet("/pool-status", async (IMessageBus bus, CancellationToken ct) =>
            {
                var result = await bus.InvokeAsync<ShufflePoolStatusResponse>(new GetShufflePoolStatusQuery(), ct);
                return Results.Ok(result);
            })
            .WithName("GetShufflePoolStatus")
            .WithSummary("Gets live Redis Shuffle Engine pool and Safety Net statistics.")
            .Produces<ShufflePoolStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/warm-cache", async (
                [FromBody] WarmShuffleCacheRequest? request,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var command = new WarmShuffleCacheCommand(
                    request?.ForceRebuild ?? false,
                    request?.TriggerReason);

                var result = await bus.InvokeAsync<WarmShuffleCacheResponse>(command, ct);
                return Results.Ok(result);
            })
            .WithName("WarmShuffleCache")
            .WithSummary("Triggers cold-start or forced Redis cache warming from Catalog gRPC.")
            .Produces<WarmShuffleCacheResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
