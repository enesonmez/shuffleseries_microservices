using MediatR;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Responses;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;

public sealed class GetAdminMediaListQueryHandler : IRequestHandler<GetAdminMediaListQuery, PaginatedList<AdminMediaListItemResponse>>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ILanguageContext _languageContext;

    public GetAdminMediaListQueryHandler(
        IMediaRepository mediaRepository,
        ILanguageContext languageContext)
    {
        _mediaRepository = mediaRepository;
        _languageContext = languageContext;
    }

    public async Task<PaginatedList<AdminMediaListItemResponse>> Handle(GetAdminMediaListQuery request, CancellationToken cancellationToken)
    {
        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;

        var (mediaItems, totalCount) = await _mediaRepository.GetAdminMediaPagedListAsync(
            request.MediaType,
            request.Search,
            request.PlatformId,
            request.MoodId,
            request.GenreId,
            request.IsDeleted,
            request.SortBy,
            request.SortDescending,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var responseItems = mediaItems.Select(m =>
        {
            var localized = m.GetLocalizedTranslation(targetLanguage);
            var title = localized?.Title ?? string.Empty;
            var overview = localized?.Overview ?? string.Empty;

            var platformNames = m.MediaPlatforms
                .Where(p => p.Platform != null)
                .Select(p => p.Platform!.Name)
                .ToList();

            var moodCodes = m.MediaMoods
                .Where(mm => mm.Mood != null)
                .Select(mm => mm.Mood!.Code)
                .ToList();

            var genreCodes = m.MediaGenres
                .Where(mg => mg.Genre != null)
                .Select(mg => mg.Genre!.Code)
                .ToList();

            return new AdminMediaListItemResponse(
                m.Id,
                m.MediaType,
                m.TmdbId,
                m.ImdbId,
                title,
                overview,
                m.ReleaseYear,
                m.PosterUrl,
                m.ImdbRating,
                m.ImdbVotes,
                platformNames,
                moodCodes,
                genreCodes,
                m.CreatedAtUtc,
                m.IsDeleted);
        }).ToList();

        return new PaginatedList<AdminMediaListItemResponse>(
            responseItems,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
