using Grpc.Core;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Exceptions;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Domain.Services;
using ShuffleSeries.ShuffleEngine.Infrastructure.Grpc.Catalog;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Services;

internal sealed class CatalogSyncGrpcClient(
    CatalogGrpc.CatalogGrpcClient grpcClient,
    TimeProvider timeProvider,
    ILogger<CatalogSyncGrpcClient> logger) : ICatalogSyncGrpcClient
{
    public async Task<ShuffleMediaSnapshot?> GetMediaByIdAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await grpcClient.GetMediaByIdAsync(
                new GetMediaByIdRequest { MediaId = mediaId.ToString() },
                cancellationToken: cancellationToken);

            return MapMediaSnapshot(response, timeProvider.GetUtcNow().Year);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "gRPC GetMediaById failed for MediaId={MediaId} with Status={StatusCode}.", mediaId, ex.StatusCode);
            }

            throw new CatalogSyncFailedException($"GetMediaById({mediaId}) returned {ex.StatusCode}");
        }
    }

    public async Task<ShuffleEpisodeSnapshot?> GetStandaloneEpisodeByIdAsync(
        Guid episodeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await grpcClient.GetStandaloneEpisodeByIdAsync(
                new GetStandaloneEpisodeByIdRequest { EpisodeId = episodeId.ToString() },
                cancellationToken: cancellationToken);

            return MapEpisodeSnapshot(response, timeProvider.GetUtcNow().Year);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "gRPC GetStandaloneEpisodeById failed for EpisodeId={EpisodeId} with Status={StatusCode}.", episodeId, ex.StatusCode);
            }

            throw new CatalogSyncFailedException($"GetStandaloneEpisodeById({episodeId}) returned {ex.StatusCode}");
        }
    }

    public async Task<IReadOnlyList<ShuffleEpisodeSnapshot>> GetStandaloneEpisodesBySeriesIdAsync(
        Guid seriesId,
        CancellationToken cancellationToken = default)
    {
        var allEpisodes = new List<ShuffleEpisodeSnapshot>();
        var page = 1;
        var pageSize = PaginationRequest.MaxPageSize;
        var currentYear = timeProvider.GetUtcNow().Year;

        try
        {
            while (true)
            {
                var response = await grpcClient.GetStandaloneEpisodesForRouletteAsync(
                    new GetStandaloneEpisodesForRouletteRequest
                    {
                        PageNumber = page,
                        PageSize = pageSize,
                        SeriesId = seriesId.ToString()
                    },
                    cancellationToken: cancellationToken);

                if (response.Items.Count == 0)
                {
                    break;
                }

                for (var i = 0; i < response.Items.Count; i++)
                {
                    allEpisodes.Add(MapEpisodeSnapshot(response.Items[i], currentYear));
                }

                if (page * pageSize >= response.TotalCount)
                {
                    break;
                }

                page++;
            }

            return allEpisodes;
        }
        catch (RpcException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "gRPC GetStandaloneEpisodesBySeriesId failed for SeriesId={SeriesId}.", seriesId);
            }

            throw new CatalogSyncFailedException($"GetStandaloneEpisodesForRoulette(SeriesId={seriesId}) returned {ex.StatusCode}");
        }
    }

    public async Task<CatalogPagedResult<ShuffleMediaSnapshot>> GetBulkMediaPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clampedPageSize = Math.Clamp(pageSize, 1, PaginationRequest.MaxPageSize);
            var clampedPageNumber = Math.Max(1, pageNumber);
            var currentYear = timeProvider.GetUtcNow().Year;

            var response = await grpcClient.GetBulkMediaForShuffleAsync(
                new GetBulkMediaForShuffleRequest
                {
                    PageNumber = clampedPageNumber,
                    PageSize = clampedPageSize
                },
                cancellationToken: cancellationToken);

            var items = new List<ShuffleMediaSnapshot>(response.Items.Count);
            for (var i = 0; i < response.Items.Count; i++)
            {
                items.Add(MapMediaSnapshot(response.Items[i], currentYear));
            }

            return new CatalogPagedResult<ShuffleMediaSnapshot>(
                items,
                response.TotalCount,
                response.PageNumber,
                response.PageSize);
        }
        catch (RpcException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "gRPC GetBulkMediaForShuffle failed on page {PageNumber}.", pageNumber);
            }

            throw new CatalogSyncFailedException($"GetBulkMediaForShuffle(Page={pageNumber}) returned {ex.StatusCode}");
        }
    }

    public async Task<CatalogPagedResult<ShuffleEpisodeSnapshot>> GetStandaloneEpisodesPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clampedPageSize = Math.Clamp(pageSize, 1, PaginationRequest.MaxPageSize);
            var clampedPageNumber = Math.Max(1, pageNumber);
            var currentYear = timeProvider.GetUtcNow().Year;

            var response = await grpcClient.GetStandaloneEpisodesForRouletteAsync(
                new GetStandaloneEpisodesForRouletteRequest
                {
                    PageNumber = clampedPageNumber,
                    PageSize = clampedPageSize
                },
                cancellationToken: cancellationToken);

            var items = new List<ShuffleEpisodeSnapshot>(response.Items.Count);
            for (var i = 0; i < response.Items.Count; i++)
            {
                items.Add(MapEpisodeSnapshot(response.Items[i], currentYear));
            }

            return new CatalogPagedResult<ShuffleEpisodeSnapshot>(
                items,
                response.TotalCount,
                response.PageNumber,
                response.PageSize);
        }
        catch (RpcException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "gRPC GetStandaloneEpisodesForRoulette failed on page {PageNumber}.", pageNumber);
            }

            throw new CatalogSyncFailedException($"GetStandaloneEpisodesForRoulette(Page={pageNumber}) returned {ex.StatusCode}");
        }
    }

    private static ShuffleMediaSnapshot MapMediaSnapshot(MediaGrpcModel model, int currentYear)
    {
        var kind = model.MediaType == 2 ? ShuffleMediaKind.Series : ShuffleMediaKind.Movie;
        double? imdbRating = model.HasImdbRating ? model.ImdbRating : null;
        int? imdbVotes = model.HasImdbVotes ? model.ImdbVotes : null;

        var baseScore = BaseQualityScoreCalculator.CalculateMediaScore(
            imdbRating,
            imdbVotes,
            model.ReleaseYear,
            currentYear);
        var isSafetyNet = BaseQualityScoreCalculator.IsSafetyNetEligible(imdbRating);

        var translations = new Dictionary<string, ShuffleLocalizedText>(
            model.Translations.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var (lang, payload) in model.Translations)
        {
            translations[lang] = new ShuffleLocalizedText(
                payload.Title,
                payload.Overview,
                payload.HasTagline ? payload.Tagline : null);
        }

        return new ShuffleMediaSnapshot(
            Id: Guid.Parse(model.Id),
            Kind: kind,
            TmdbId: model.TmdbId,
            ImdbId: model.ImdbId,
            ImdbRating: imdbRating,
            ImdbVotes: imdbVotes,
            ReleaseYear: model.ReleaseYear,
            PosterUrl: model.PosterUrl,
            BackdropUrl: model.HasBackdropUrl ? model.BackdropUrl : null,
            RuntimeMinutes: model.HasRuntimeMinutes ? model.RuntimeMinutes : null,
            Director: model.HasDirector ? model.Director : null,
            SeriesType: model.HasSeriesType ? model.SeriesType : null,
            TotalSeasons: model.HasTotalSeasons ? model.TotalSeasons : null,
            BaseQualityScore: baseScore,
            IsSafetyNetEligible: isSafetyNet,
            PlatformIds: ParseGuidList(model.PlatformIds),
            MoodIds: ParseGuidList(model.MoodIds),
            GenreIds: ParseGuidList(model.GenreIds),
            PlatformLinks: MapPlatformLinks(model.PlatformLinks),
            Translations: translations);
    }

    private static ShuffleEpisodeSnapshot MapEpisodeSnapshot(StandaloneEpisodeGrpcModel model, int currentYear)
    {
        double? imdbRating = model.HasImdbRating ? model.ImdbRating : null;
        var baseScore = BaseQualityScoreCalculator.CalculateEpisodeScore(
            imdbRating,
            model.ReleaseYear,
            currentYear);
        var isSafetyNet = BaseQualityScoreCalculator.IsSafetyNetEligible(imdbRating);

        var epTranslations = new Dictionary<string, ShuffleLocalizedText>(
            model.Translations.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var (lang, payload) in model.Translations)
        {
            epTranslations[lang] = new ShuffleLocalizedText(
                payload.Title,
                payload.HasOverview ? payload.Overview : null);
        }

        var seriesTranslations = new Dictionary<string, ShuffleLocalizedText>(
            model.SeriesTranslations.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var (lang, payload) in model.SeriesTranslations)
        {
            seriesTranslations[lang] = new ShuffleLocalizedText(
                payload.Title,
                payload.Overview,
                payload.HasTagline ? payload.Tagline : null);
        }

        return new ShuffleEpisodeSnapshot(
            EpisodeId: Guid.Parse(model.EpisodeId),
            SeriesId: Guid.Parse(model.SeriesId),
            SeasonId: Guid.Parse(model.SeasonId),
            SeasonNumber: model.SeasonNumber,
            EpisodeNumber: model.EpisodeNumber,
            RuntimeMinutes: model.HasRuntimeMinutes ? model.RuntimeMinutes : null,
            ImdbRating: imdbRating,
            ThumbnailUrl: model.HasThumbnailUrl ? model.ThumbnailUrl : null,
            SeriesPosterUrl: model.SeriesPosterUrl,
            ReleaseYear: model.ReleaseYear,
            BaseQualityScore: baseScore,
            IsSafetyNetEligible: isSafetyNet,
            PlatformIds: ParseGuidList(model.PlatformIds),
            MoodIds: ParseGuidList(model.MoodIds),
            GenreIds: ParseGuidList(model.GenreIds),
            PlatformLinks: MapPlatformLinks(model.PlatformLinks),
            Translations: epTranslations,
            SeriesTranslations: seriesTranslations);
    }

    private static List<Guid> ParseGuidList(Google.Protobuf.Collections.RepeatedField<string> rawIds)
    {
        var list = new List<Guid>(rawIds.Count);
        for (var i = 0; i < rawIds.Count; i++)
        {
            if (Guid.TryParse(rawIds[i], out var id))
            {
                list.Add(id);
            }
        }

        return list;
    }

    private static List<ShufflePlatformDeepLink> MapPlatformLinks(
        Google.Protobuf.Collections.RepeatedField<PlatformDeepLinkGrpcModel> links)
    {
        var list = new List<ShufflePlatformDeepLink>(links.Count);
        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (!Guid.TryParse(link.PlatformId, out var platformId))
            {
                continue;
            }

            list.Add(new ShufflePlatformDeepLink(
                platformId,
                link.PlatformName,
                link.PlatformSlug,
                link.LogoUrl,
                link.WebUrl,
                link.HasAppSchemeUrl ? link.AppSchemeUrl : null));
        }

        return list;
    }
}
