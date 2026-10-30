using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Moods;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.CreateMood;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.DeleteMood;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.AddOrUpdateMoodTranslation;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.RemoveMoodTranslation;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.UpdateMood;
using ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetActiveMoods;
using ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetMoodById;
using ShuffleSeries.Catalog.Application.Features.Moods.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class MoodEndpoints
{
    public static void MapMoodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/moods")
            .WithTags("Moods");

        // 1. GET: Get active moods (Cached, Localized, with Permission)
        group.MapGet("/", async (
                [FromQuery] string? lang,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetActiveMoodsQuery(lang ?? languageContext.CurrentLanguage);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogMoods)
            .WithName("GetActiveMoods")
            .WithSummary("Aktif modları listeler")
            .WithDescription("Kataloga kayıtlı aktif modları (Kafa Dağıtmalık vb.) dil desteği ve önbellekleme ile listeler.")
            .Produces<IReadOnlyList<MoodResponse>>(StatusCodes.Status200OK)
            .ProducesSecurityAndErrorProblems();

        // 2. GET: Get mood by ID (Localized, with Permission)
        group.MapGet("/{id:guid}", async (
                Guid id,
                [FromQuery] string? lang,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMoodByIdQuery(id, lang ?? languageContext.CurrentLanguage);
                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadCatalogMoods)
            .WithName("GetMoodById")
            .WithSummary("ID'ye göre mod detayını getirir")
            .Produces<MoodResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 3. POST: Create mood (Admin)
        group.MapPost("/", async (
                [FromBody] CreateMoodRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateMoodCommand(
                    request.Code,
                    request.AmbientColorHex,
                    request.Name,
                    request.Description,
                    request.LanguageCode,
                    request.VibeTags,
                    request.IconUrl,
                    request.DisplayOrder,
                    request.IsActive);

                var moodId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetMoodById", new { id = moodId }, moodId);
            })
            .RequireAuthorization(SystemPolicies.CanCreateCatalogMoods)
            .WithName("CreateMood")
            .WithSummary("Yeni bir mod oluşturur")
            .WithDescription("Yeni bir izleme modu ekler ve mod önbelleğini geçersiz kılar.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. PUT: Update mood (Admin)
        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateMoodRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateMoodCommand(
                    id,
                    request.Code,
                    request.AmbientColorHex,
                    request.IconUrl,
                    request.DisplayOrder);

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogMoods)
            .WithName("UpdateMood")
            .WithSummary("Mevcut modu günceller")
            .WithDescription("Mod kod ve görsel bilgilerini günceller ve mod önbelleğini geçersiz kılar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesSecurityAndErrorProblems();

        // 4. DELETE: Delete mood (Admin)
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteMoodCommand(id);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanDeleteCatalogMoods)
            .WithName("DeleteMood")
            .WithSummary("Modu mantıksal olarak siler")
            .WithDescription("Modu soft-delete ile siler ve mod önbelleğini geçersiz kılar.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 5. POST: Add or update mood translation
        group.MapPost("/{id:guid}/translations", async (
                Guid id,
                [FromBody] AddOrUpdateMoodTranslationRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new AddOrUpdateMoodTranslationCommand(id, request.LanguageCode, request.Name, request.Description, request.VibeTags);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogMoods)
            .WithName("AddOrUpdateMoodTranslation")
            .WithSummary("Moda çeviri ekler veya günceller")
            .WithDescription("Belirtilen dil kodunda mod adı, açıklaması ve vibe etiketlerini ekler ya da günceller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();

        // 6. DELETE: Remove mood translation
        group.MapDelete("/{id:guid}/translations/{languageCode}", async (
                Guid id,
                string languageCode,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveMoodTranslationCommand(id, languageCode);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(SystemPolicies.CanUpdateCatalogMoods)
            .WithName("RemoveMoodTranslation")
            .WithSummary("Moddan dil çevirisini kaldırır")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesSecurityAndErrorProblems();
    }
}
