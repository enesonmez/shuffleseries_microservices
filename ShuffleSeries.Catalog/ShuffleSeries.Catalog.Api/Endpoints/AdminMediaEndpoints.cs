using ShuffleSeries.Catalog.Api.Contracts.Requests.AdminMedia;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Responses;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Application.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Web.Extensions;
using Wolverine;

namespace ShuffleSeries.Catalog.Api.Endpoints;

public static class AdminMediaEndpoints
{
    public static void MapAdminMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog/admin/media")
            .WithTags("Admin Media Management");

        // 1. GET: Get paginated and filterable media list (Admin CMS)
        group.MapGet("/", async (
                [AsParameters] GetAdminMediaListRequest request,
                IMessageBus bus,
                ILanguageContext languageContext,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAdminMediaListQuery
                {
                    MediaType = request.MediaType,
                    Search = request.Search,
                    PlatformId = request.PlatformId,
                    MoodId = request.MoodId,
                    GenreId = request.GenreId,
                    PageNumber = request.Page ?? PaginationRequest.DefaultPageNumber,
                    PageSize = request.PageSize ?? PaginationRequest.DefaultPageSize,
                    Language = languageContext.CurrentLanguage,
                    IsDeleted = request.IsDeleted ?? false,
                    SortBy = request.SortBy,
                    SortDescending = request.SortDescending ?? true
                };

                var response = await bus.InvokeAsync<PaginatedList<AdminMediaListItemResponse>>(query, cancellationToken);
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
