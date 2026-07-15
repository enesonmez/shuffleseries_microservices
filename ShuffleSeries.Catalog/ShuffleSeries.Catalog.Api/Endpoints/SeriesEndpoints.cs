using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class SeriesEndpoints
{
    public static void MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/series");
        
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
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Create series endpoint";
                operation.Description = "Create a new series";
                return Task.CompletedTask;
            });
        
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
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Summary = "Get series by ID endpoint";
            operation.Description = "Get a series by ID";
            return Task.CompletedTask;
        });
        
        // 3. PUT: Update series endpoint (Command)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSeriesCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (id != command.Id)
                return Results.BadRequest(new { Message = "Route ID and Command ID must match." });
            
            await sender.Send(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("UpdateSeries")
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Summary = "Update series endpoint";
            operation.Description = "Update an existing series by its ID";
            return Task.CompletedTask;
        });
        
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
        .AddOpenApiOperationTransformer((operation, context, ct) =>
        {
            operation.Summary = "Delete series endpoint";
            operation.Description = "Delete an existing series by its ID";
            return Task.CompletedTask;
        });
    }
}