using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Series;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;
using ShuffleSeries.Catalog.Application.Features.Series.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class SeriesEndpoints
{
    public static void MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/series")
            .WithTags("Series");

        // 1. POST: Create a series endpoint (Command)
        group.MapPost("/", async (
                [FromBody] CreateSeriesRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateSeriesCommand(
                    request.TmdbId,
                    request.ImdbId,
                    request.ReleaseYear,
                    request.SeriesType,
                    request.PosterUrl,
                    request.BackdropUrl,
                    request.LanguageCode,
                    request.Title,
                    request.Overview,
                    request.Tagline,
                    request.ImdbRating,
                    request.ImdbVotes,
                    request.TotalSeasons,
                    request.PlatformIds,
                    request.MoodIds,
                    request.GenreIds);

                var seriesId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetSeriesById", new { id = seriesId }, seriesId);
            })
            .RequireAuthorization(SystemPolicies.CanCreateCatalogSeries)
            .WithName("CreateSeries")
            .WithSummary("Yeni bir dizi oluşturur")
            .WithDescription("Katalog bounded context'ine yeni bir dizi ekler. Başlık tekilliğini denetler ve Transactional Outbox üzerinden MediaCreatedEvent yayınlar.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 2. GET: Get series by ID endpoint (Query)
        group.MapGet("/{id:guid}", async (
                Guid id,
                [FromQuery] string? lang,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetSeriesByIdQuery(id, lang ?? languageContext.CurrentLanguage);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogSeries)
            .WithName("GetSeriesById")
            .WithSummary("ID'ye göre dizi detayını getirir")
            .WithDescription("Belirtilen benzersiz kimliğe (GUID) sahip dizinin detay bilgilerini sorgular. Soft-delete edilmiş kayıtlar filtrelenir.")
            .Produces<SeriesDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 3. PUT: Update series endpoint (Command)
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateSeriesRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateSeriesCommand(
                    id,
                    request.TmdbId,
                    request.ImdbId,
                    request.ReleaseYear,
                    request.SeriesType,
                    request.TotalSeasons,
                    request.ImdbRating,
                    request.ImdbVotes,
                    request.PosterUrl,
                    request.BackdropUrl);

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogSeries)
            .WithName("UpdateSeries")
            .WithSummary("Mevcut bir diziyi günceller")
            .WithDescription("Dizinin başlık, açıklama, yayın yılı gibi bilgilerini günceller ve MediaUpdatedEvent yayınlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. DELETE: Delete series endpoint (Command)
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteSeriesCommand(id);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanDeleteCatalogSeries)
            .WithName("DeleteSeries")
            .WithSummary("Diziyi mantıksal (Soft Delete) olarak siler")
            .WithDescription("Diziyi IsDeleted flag'i ile işaretleyerek mantıksal olarak siler ve MediaDeletedEvent yayınlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
