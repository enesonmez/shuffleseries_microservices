using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Genres;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.CreateGenre;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.DeleteGenre;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.AddOrUpdateGenreTranslation;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.Translations.RemoveGenreTranslation;
using ShuffleSeries.Catalog.Application.Features.Genres.Commands.UpdateGenre;
using ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetActiveGenres;
using ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetGenreById;
using ShuffleSeries.Catalog.Application.Features.Genres.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class GenreEndpoints
{
    public static void MapGenreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/genres")
            .WithTags("Genres");

        // 1. GET: Get active genres (Cached, Localized, with Permission)
        group.MapGet("/", async (
                [FromQuery] string? lang,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetActiveGenresQuery(lang ?? languageContext.CurrentLanguage);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogGenres)
            .WithName("GetActiveGenres")
            .WithSummary("Aktif türleri listeler")
            .WithDescription("Kataloga kayıtlı aktif film/dizi türlerini dil desteği ve önbellekleme ile listeler.")
            .Produces<IReadOnlyList<GenreResponse>>(StatusCodes.Status200OK)
            .ProducesSecurityAndErrorProblems();

        // 2. GET: Get genre by ID (Localized, with Permission)
        group.MapGet("/{id:guid}", async (
                Guid id,
                [FromQuery] string? lang,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetGenreByIdQuery(id, lang ?? languageContext.CurrentLanguage);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogGenres)
            .WithName("GetGenreById")
            .WithSummary("ID'ye göre tür detayını getirir")
            .Produces<GenreResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 3. POST: Create genre (Admin)
        group.MapPost("/", async (
                [FromBody] CreateGenreRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateGenreCommand(
                    request.Code,
                    request.Name,
                    request.LanguageCode,
                    request.Description,
                    request.DisplayOrder,
                    request.IsActive);

                var genreId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetGenreById", new { id = genreId }, genreId);
            })
            .RequireAuthorization(SystemPolicies.CanCreateCatalogGenres)
            .WithName("CreateGenre")
            .WithSummary("Yeni bir tür oluşturur")
            .WithDescription("Yeni bir içerik türü ekler ve tür önbelleğini geçersiz kılar.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. PUT: Update genre (Admin)
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateGenreRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateGenreCommand(
                    id,
                    request.Code,
                    request.DisplayOrder);

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogGenres)
            .WithName("UpdateGenre")
            .WithSummary("Mevcut türü günceller")
            .WithDescription("Tür kodunu ve sıralamasını günceller ve tür önbelleğini geçersiz kılar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. DELETE: Delete genre (Admin)
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteGenreCommand(id);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanDeleteCatalogGenres)
            .WithName("DeleteGenre")
            .WithSummary("Türü mantıksal olarak siler")
            .WithDescription("Türü soft-delete ile siler ve tür önbelleğini geçersiz kılar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 5. POST: Add or update genre translation
        group.MapPost("/{id:guid}/translations", async (
                Guid id,
                [FromBody] AddOrUpdateGenreTranslationRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new AddOrUpdateGenreTranslationCommand(id, request.LanguageCode, request.Name, request.Description);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogGenres)
            .WithName("AddOrUpdateGenreTranslation")
            .WithSummary("Türe çeviri ekler veya günceller")
            .WithDescription("Belirtilen dil kodunda tür adı ve açıklamasını ekler ya da günceller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 6. DELETE: Remove genre translation
        group.MapDelete("/{id:guid}/translations/{languageCode}", async (
                Guid id,
                string languageCode,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveGenreTranslationCommand(id, languageCode);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogGenres)
            .WithName("RemoveGenreTranslation")
            .WithSummary("Türden dil çevirisini kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
