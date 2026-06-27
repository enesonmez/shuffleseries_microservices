using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class SeriesEndpoints
{
    public static void MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/series");
        
        // 1. POST: Dizi Ekleme (Command)
        group.MapPost("/", async (
                [FromBody] CreateSeriesCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var seriesId = await sender.Send(command, cancellationToken);

                return Results.CreatedAtRoute("GetSeriesById", new { id = seriesId }, seriesId);
            })
            .WithName("CreateSeries")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Create series endpoint";
                operation.Description = "Create a new series";
                return Task.CompletedTask;
            });
        
        // 2. GET: Id'ye Göre Dizi Detayı Getirme (Query)
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSeriesByIdQuery(id);
            var response = await sender.Send(query, cancellationToken);
            
            return response is null 
                ? Results.NotFound(new { Message = $"Series with ID {id} was not found." }) 
                : Results.Ok(response);
        })
        .WithName("GetSeriesById")
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Summary = "Get series by ID endpoint";
            operation.Description = "Get a series by ID";
            return Task.CompletedTask;
        });
    }
}