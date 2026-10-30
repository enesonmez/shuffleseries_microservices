using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Responses;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Application.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class AdminMediaEndpoints
{
    public static void MapAdminMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/admin/media")
            .WithTags("Admin Media Management");

        // 1. GET: Get paginated and filterable media list (Admin CMS)
        group.MapGet("/", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? search,
                [FromQuery] MediaType? mediaType,
                [FromQuery] Guid? platformId,
                [FromQuery] Guid? moodId,
                [FromQuery] Guid? genreId,
                [FromQuery] string? lang,
                [FromQuery] bool? isDeleted,
                [FromQuery] string? sortBy,
                [FromQuery] bool? sortDescending,
                ISender sender,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAdminMediaListQuery(
                    mediaType: mediaType,
                    search: search,
                    platformId: platformId,
                    moodId: moodId,
                    genreId: genreId,
                    pageNumber: page ?? 1,
                    pageSize: pageSize ?? 10,
                    language: lang ?? languageContext.CurrentLanguage,
                    isDeleted: isDeleted ?? false,
                    sortBy: sortBy,
                    sortDescending: sortDescending ?? true);

                var response = await sender.Send(query, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(SystemPolicies.CanReadAdminMedia)
            .WithName("GetAdminMediaList")
            .WithSummary("CMS için sayfalanmış ve filtrelenebilir medya listesini döner")
            .WithDescription("Yönetici paneli için film ve dizileri tür, mod, platform ve arama kriterlerine göre filtreler.")
            .Produces<PaginatedList<AdminMediaListItemResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesSecurityAndErrorProblems();
    }
}
