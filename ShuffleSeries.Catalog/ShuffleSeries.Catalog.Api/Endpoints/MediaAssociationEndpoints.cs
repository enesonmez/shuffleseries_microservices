using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.MediaAssociations;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.RemoveMediaMood;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.AddMediaPlatform;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Platforms.RemoveMediaPlatform;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.AddOrUpdateMediaTranslation;
using ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Translations.RemoveMediaTranslation;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;
using Wolverine;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class MediaAssociationEndpoints
{
    public static void MapMediaAssociationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/media/{id:guid}")
            .WithTags("Media Associations & Translations");

        // --- PLATFORM ASSOCIATIONS ---
        group.MapPost("/platforms", async (
                Guid id,
                [FromBody] AddMediaPlatformRequest request,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new AddMediaPlatformCommand(id, request.PlatformId, request.WebUrl, request.AppSchemeUrl);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("AddMediaPlatform")
            .WithSummary("Medyaya yayın platformu bağlar")
            .WithDescription("İçeriğin (Film/Dizi) yayınlandığı platform linkini ve uygulamasını tanımlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        group.MapDelete("/platforms/{platformId:guid}", async (
                Guid id,
                Guid platformId,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveMediaPlatformCommand(id, platformId);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("RemoveMediaPlatform")
            .WithSummary("Medyadan yayın platformu bağını kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // --- MOOD ASSOCIATIONS ---
        group.MapPost("/moods", async (
                Guid id,
                [FromBody] AddMediaMoodRequest request,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new AddMediaMoodCommand(id, request.MoodId);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("AddMediaMood")
            .WithSummary("Medyaya mod etiketi bağlar")
            .WithDescription("İçeriğe Shuffle algoritmasının kullanacağı duygu/mod etiketini bağlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        group.MapDelete("/moods/{moodId:guid}", async (
                Guid id,
                Guid moodId,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveMediaMoodCommand(id, moodId);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("RemoveMediaMood")
            .WithSummary("Medyadan mod etiketini kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // --- GENRE ASSOCIATIONS ---
        group.MapPost("/genres", async (
                Guid id,
                [FromBody] AddMediaGenreRequest request,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new AddMediaGenreCommand(id, request.GenreId);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("AddMediaGenre")
            .WithSummary("Medyaya içerik türü bağlar")
            .WithDescription("İçeriğe film/dizi türü bağlar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        group.MapDelete("/genres/{genreId:guid}", async (
                Guid id,
                Guid genreId,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveMediaGenreCommand(id, genreId);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("RemoveMediaGenre")
            .WithSummary("Medyadan içerik türünü kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // --- TRANSLATION ASSOCIATIONS ---
        group.MapPost("/translations", async (
                Guid id,
                [FromBody] AddOrUpdateTranslationRequest request,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new AddOrUpdateMediaTranslationCommand(
                    id,
                    request.LanguageCode,
                    request.Title,
                    request.Overview,
                    request.Tagline);

                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("AddOrUpdateMediaTranslation")
            .WithSummary("Medyaya yerelleştirilmiş çeviri ekler veya günceller")
            .WithDescription("Belirtilen dildeki (tr, en vb.) başlık ve özet çevirisini kaydeder veya günceller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        group.MapDelete("/translations/{languageCode}", async (
                Guid id,
                string languageCode,
                IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveMediaTranslationCommand(id, languageCode);
                await bus.InvokeAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanManageCatalogAssociations)
            .WithName("RemoveMediaTranslation")
            .WithSummary("Medyadan belirtilen dildeki çeviriyi kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
