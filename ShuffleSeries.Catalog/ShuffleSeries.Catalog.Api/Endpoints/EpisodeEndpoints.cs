using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Episodes;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.CreateEpisode;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.DeleteEpisode;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.UpdateEpisode;
using ShuffleSeries.Catalog.Application.Features.Episodes.Queries.GetEpisodeById;
using ShuffleSeries.Catalog.Application.Features.Episodes.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class EpisodeEndpoints
{
    public static void MapEpisodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/episodes")
            .WithTags("Episodes");

        // 1. POST: Create episode
        group.MapPost("/", async (
                [FromBody] CreateEpisodeRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateEpisodeCommand(
                    request.SeriesId,
                    request.SeasonNumber,
                    request.EpisodeNumber,
                    request.Title,
                    request.Overview,
                    request.LanguageCode,
                    request.IsStandalone,
                    request.RuntimeMinutes,
                    request.ImdbRating,
                    request.ThumbnailUrl);

                var episodeId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetEpisodeById", new { id = episodeId }, episodeId);
            })
            .RequireAuthorization(SystemPolicies.CanCreateCatalogEpisodes)
            .WithName("CreateEpisode")
            .WithSummary("Diziye yeni bir bölüm ekler")
            .WithDescription("Belirtilen dizi ve sezona yeni bir bölüm ekler. Sezon yoksa otomatik oluşturur.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 2. GET: Get episode by ID (with Content Negotiation & Permission)
        group.MapGet("/{id:guid}", async (
                Guid id,
                [FromQuery] string? lang,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetEpisodeByIdQuery(id, lang ?? languageContext.CurrentLanguage);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogEpisodes)
            .WithName("GetEpisodeById")
            .WithSummary("ID'ye göre bölüm detayını getirir")
            .WithDescription("Bölüm zengin detaylarını ve çevirisini döner. ?lang= ve Accept-Language destekler.")
            .Produces<EpisodeDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 3. PUT: Update episode
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateEpisodeRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateEpisodeCommand(
                    id,
                    request.EpisodeNumber,
                    request.IsStandalone,
                    request.RuntimeMinutes,
                    request.ImdbRating,
                    request.ThumbnailUrl);

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogEpisodes)
            .WithName("UpdateEpisode")
            .WithSummary("Bölüm meta verilerini günceller")
            .WithDescription("Bölüm numarası, süre, standalone bayrağı ve IMDb puanını günceller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. DELETE: Delete episode
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteEpisodeCommand(id);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanDeleteCatalogEpisodes)
            .WithName("DeleteEpisode")
            .WithSummary("Bölümü mantıksal olarak siler")
            .WithDescription("Bölümü soft-delete ile siler.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 5. POST: Add episode platform
        group.MapPost("/{id:guid}/platforms", async (
                Guid id,
                [FromBody] AddEpisodePlatformRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new AddEpisodePlatformCommand(id, request.PlatformId, request.WebUrl, request.AppSchemeUrl);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("AddEpisodePlatform")
            .WithSummary("Bölüme yayın platformu bağlar")
            .WithDescription("Bölümün platformdaki doğrudan oynatıcı linkini (Deep Link) tanımlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 6. DELETE: Remove episode platform
        group.MapDelete("/{id:guid}/platforms/{platformId:guid}", async (
                Guid id,
                Guid platformId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveEpisodePlatformCommand(id, platformId);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("RemoveEpisodePlatform")
            .WithSummary("Bölümden yayın platformu bağını kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 7. POST: Add or update episode translation
        group.MapPost("/{id:guid}/translations", async (
                Guid id,
                [FromBody] AddOrUpdateEpisodeTranslationRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new AddOrUpdateEpisodeTranslationCommand(id, request.LanguageCode, request.Title, request.Overview);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("AddOrUpdateEpisodeTranslation")
            .WithSummary("Bölüme çeviri ekler veya günceller")
            .WithDescription("Belirtilen dil kodunda bölüm başlığı ve özetini ekler ya da günceller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 8. DELETE: Remove episode translation
        group.MapDelete("/{id:guid}/translations/{languageCode}", async (
                Guid id,
                string languageCode,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveEpisodeTranslationCommand(id, languageCode);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("RemoveEpisodeTranslation")
            .WithSummary("Bölümden dil çevirisini kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
