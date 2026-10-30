using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Platforms;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.CreatePlatform;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.DeletePlatform;
using ShuffleSeries.Catalog.Application.Features.Platforms.Commands.UpdatePlatform;
using ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetActivePlatforms;
using ShuffleSeries.Catalog.Application.Features.Platforms.Queries.GetPlatformById;
using ShuffleSeries.Catalog.Application.Features.Platforms.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class PlatformEndpoints
{
    public static void MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/platforms")
            .WithTags("Platforms");

        // 1. GET: Get active platforms (Cached, with Permission)
        group.MapGet("/", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetActivePlatformsQuery();
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogPlatforms)
            .WithName("GetActivePlatforms")
            .WithSummary("Aktif platformları listeler")
            .WithDescription("Kataloga kayıtlı aktif platformları (Netflix, Prime vb.) listeler. Read-Through Memory Cache ile önbelleklenir.")
            .Produces<IReadOnlyList<PlatformResponse>>(StatusCodes.Status200OK)
            .ProducesSecurityAndErrorProblems();

        // 2. GET: Get platform by ID
        group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetPlatformByIdQuery(id);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogPlatforms)
            .WithName("GetPlatformById")
            .WithSummary("ID'ye göre platform detayını getirir")
            .Produces<PlatformResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 3. POST: Create platform (Admin)
        group.MapPost("/", async (
                [FromBody] CreatePlatformRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreatePlatformCommand(
                    request.Name,
                    request.Slug,
                    request.LogoUrl,
                    request.InactiveLogoUrl,
                    request.BaseWebUrl,
                    request.BaseAppSchemeUrl,
                    request.DisplayOrder,
                    request.IsActive);

                var platformId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetPlatformById", new { id = platformId }, platformId);
            })
            .RequireAuthorization(SystemPolicies.CanCreateCatalogPlatforms)
            .WithName("CreatePlatform")
            .WithSummary("Yeni bir platform oluşturur")
            .WithDescription("Yeni bir yayın platformu ekler ve platform önbelleğini geçersiz kılar.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. PUT: Update platform (Admin)
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdatePlatformRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdatePlatformCommand(
                    id,
                    request.Name,
                    request.Slug,
                    request.LogoUrl,
                    request.InactiveLogoUrl,
                    request.BaseWebUrl,
                    request.BaseAppSchemeUrl,
                    request.DisplayOrder);

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogPlatforms)
            .WithName("UpdatePlatform")
            .WithSummary("Mevcut platformu günceller")
            .WithDescription("Platform bilgilerini günceller ve platform önbelleğini geçersiz kılar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. DELETE: Delete platform (Admin)
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new DeletePlatformCommand(id);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanDeleteCatalogPlatforms)
            .WithName("DeletePlatform")
            .WithSummary("Platformu mantıksal olarak siler")
            .WithDescription("Platformu soft-delete ile siler ve platform önbelleğini geçersiz kılar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
