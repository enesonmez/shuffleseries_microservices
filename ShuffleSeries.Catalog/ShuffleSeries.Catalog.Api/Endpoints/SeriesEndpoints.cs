using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesList;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class SeriesEndpoints
{
    public static void MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/series")
            .WithTags("Series");

        // 1. POST: Create a series endpoint (Command)
        group.MapPost("/", async (
                [FromBody] CreateSeriesCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var seriesId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetSeriesById", new { id = seriesId }, seriesId);
            })
            .WithName("CreateSeries")
            .WithSummary("Yeni bir dizi oluşturur")
            .WithDescription("Katalog bounded context'ine yeni bir dizi ekler. Başlık tekilliğini denetler ve Transactional Outbox üzerinden MediaCreatedEvent yayınlar.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 2. GET: Get series by ID endpoint (Query)
        group.MapGet("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetSeriesByIdQuery(id);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("GetSeriesById")
            .WithSummary("ID'ye göre dizi detayını getirir")
            .WithDescription("Belirtilen benzersiz kimliğe (GUID) sahip dizinin detay bilgilerini sorgular. Soft-delete edilmiş kayıtlar filtrelenir.")
            .Produces<SeriesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 3. PUT: Update series endpoint (Command)
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateSeriesCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest(new { Message = "Route ID and Command ID must match." });
                }

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("UpdateSeries")
            .WithSummary("Mevcut bir diziyi günceller")
            .WithDescription("Dizinin başlık, açıklama, yayın yılı gibi bilgilerini günceller ve MediaUpdatedEvent yayınlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

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
            .WithName("DeleteSeries")
            .WithSummary("Diziyi mantıksal (Soft Delete) olarak siler")
            .WithDescription("Diziyi IsDeleted flag'i ile işaretleyerek mantıksal olarak siler ve MediaDeletedEvent yayınlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // 5. GET: Get paginated series list endpoint (Query)
        group.MapGet("/", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetSeriesListQuery(page, pageSize);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .WithName("GetSeriesList")
            .WithSummary("Sayfalanmış dizi listesini döner")
            .WithDescription("Aktif (silinmemiş) tüm dizileri sayfalanmış (PaginatedList) formatta döner.")
            .Produces<PaginatedList<SeriesListItemResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}