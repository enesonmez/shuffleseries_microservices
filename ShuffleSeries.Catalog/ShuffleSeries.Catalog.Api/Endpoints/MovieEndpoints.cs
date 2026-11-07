using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Movies;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.DeleteMovie;
using ShuffleSeries.Catalog.Application.Features.Movies.Commands.UpdateMovie;
using ShuffleSeries.Catalog.Application.Features.Movies.Queries.GetMovieById;
using ShuffleSeries.Catalog.Application.Features.Movies.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;
using Wolverine;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class MovieEndpoints
{
    public static void MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/movies")
            .WithTags("Movies");

        // 1. POST: Create a movie endpoint (Admin/Catalog Manager)
        group.MapPost("/", async (
                [FromBody] CreateMovieRequest request,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateMovieCommand(
                    request.TmdbId,
                    request.ImdbId,
                    request.ReleaseYear,
                    request.RuntimeMinutes,
                    request.PosterUrl,
                    request.BackdropUrl,
                    request.LanguageCode,
                    request.Title,
                    request.Overview,
                    request.Tagline,
                    request.Director,
                    request.ImdbRating,
                    request.ImdbVotes,
                    request.PlatformIds,
                    request.MoodIds,
                    request.GenreIds);

                var movieId = await bus.InvokeAsync<Guid>(command, cancellationToken);
                return Results.CreatedAtRoute("GetMovieById", new { id = movieId }, movieId);
            })
            .RequireAuthorization(SystemPolicies.CanCreateCatalogMovies)
            .WithName("CreateMovie")
            .WithSummary("Yeni bir film oluşturur")
            .WithDescription("Kataloga yeni bir film ekler. Başlık ve IMDb tekilliğini doğrular.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 2. GET: Get movie by ID endpoint (with Content Negotiation & Permission)
        group.MapGet("/{id:guid}", async (
                Guid id,
                IMessageBus bus,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMovieByIdQuery(id, languageContext.CurrentLanguage);
                var response = await bus.InvokeAsync<MovieDetailResponse>(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogMovies)
            .WithName("GetMovieById")
            .WithSummary("ID'ye göre film detayını getirir")
            .WithDescription("Film zengin detaylarını (çeviri, platform, mood ve türler) döner. ?lang= ve Accept-Language destekler.")
            .Produces<MovieDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 3. PUT: Update movie endpoint (Admin/Catalog Manager)
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateMovieRequest request,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateMovieCommand(
                    id,
                    request.TmdbId,
                    request.ImdbId,
                    request.ReleaseYear,
                    request.RuntimeMinutes,
                    request.Director,
                    request.ImdbRating,
                    request.ImdbVotes,
                    request.PosterUrl,
                    request.BackdropUrl);

                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogMovies)
            .WithName("UpdateMovie")
            .WithSummary("Mevcut bir filmi günceller")
            .WithDescription("Filmin yayın yılı, süre, yönetmen ve IMDb bilgilerini günceller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. DELETE: Delete movie endpoint (Admin/Catalog Manager)
        group.MapDelete("/{id:guid}", async (
                Guid id,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteMovieCommand(id);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanDeleteCatalogMovies)
            .WithName("DeleteMovie")
            .WithSummary("Filmi mantıksal olarak siler")
            .WithDescription("Filmi soft-delete ile siler ve MediaDeletedDomainEvent yayınlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
