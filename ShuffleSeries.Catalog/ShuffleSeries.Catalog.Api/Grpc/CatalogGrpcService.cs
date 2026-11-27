using Grpc.Core;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetBulkMediaForShuffle;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetMediaByIdForShuffle;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Queries.GetStandaloneEpisodesForRoulette;
using ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Application.Responses;
using Wolverine;

namespace ShuffleSeries.Catalog.Api.Grpc;

public sealed class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<CatalogGrpcService> _logger;

    public CatalogGrpcService(IMessageBus messageBus, ILogger<CatalogGrpcService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public override async Task<GetBulkMediaForShuffleResponse> GetBulkMediaForShuffle(
        GetBulkMediaForShuffleRequest request,
        ServerCallContext context)
    {
        MediaType? mediaTypeFilter = null;
        if (request.HasMediaType)
        {
            var candidateMediaType = (MediaType)request.MediaType;
            if (!Enum.IsDefined(candidateMediaType))
            {
                throw new RpcException(new Status(
                    StatusCode.InvalidArgument,
                    "Invalid media_type. Supported values: 1 (Movie), 2 (Series)."));
            }

            mediaTypeFilter = candidateMediaType;
        }

        var query = new GetBulkMediaForShuffleQuery(request.PageNumber, request.PageSize, mediaTypeFilter);
        var result = await InvokeSafelyAsync(
            () => _messageBus.InvokeAsync<PaginatedList<ShuffleMediaItemResponse>>(query, context.CancellationToken),
            nameof(GetBulkMediaForShuffle),
            context.CancellationToken);

        var response = new GetBulkMediaForShuffleResponse
        {
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        response.Items.AddRange(result.Items.Select(MapToGrpcModel));
        return response;
    }

    public override async Task<GetStandaloneEpisodesForRouletteResponse> GetStandaloneEpisodesForRoulette(
        GetStandaloneEpisodesForRouletteRequest request,
        ServerCallContext context)
    {
        var query = new GetStandaloneEpisodesForRouletteQuery(request.PageNumber, request.PageSize);
        var result = await InvokeSafelyAsync(
            () => _messageBus.InvokeAsync<PaginatedList<ShuffleStandaloneEpisodeResponse>>(query, context.CancellationToken),
            nameof(GetStandaloneEpisodesForRoulette),
            context.CancellationToken);

        var response = new GetStandaloneEpisodesForRouletteResponse
        {
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };

        response.Items.AddRange(result.Items.Select(MapToGrpcModel));
        return response;
    }

    public override async Task<MediaGrpcModel> GetMediaById(
        GetMediaByIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.MediaId, out var mediaId) || mediaId == Guid.Empty)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Invalid media_id format. A non-empty GUID is required."));
        }

        var query = new GetMediaByIdForShuffleQuery(mediaId);
        var item = await InvokeSafelyAsync(
            () => _messageBus.InvokeAsync<ShuffleMediaItemResponse>(query, context.CancellationToken),
            nameof(GetMediaById),
            context.CancellationToken);

        return MapToGrpcModel(item);
    }

    private async Task<T> InvokeSafelyAsync<T>(
        Func<Task<T>> operation,
        string methodName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation();
        }
        catch (MediaNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled by the client."));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CatalogGrpcService.{MethodName}", methodName);
            throw new RpcException(new Status(
                StatusCode.Internal,
                "An unexpected error occurred while processing the catalog gRPC request."));
        }
    }

    private static MediaGrpcModel MapToGrpcModel(ShuffleMediaItemResponse item)
    {
        var model = new MediaGrpcModel
        {
            Id = item.Id.ToString(),
            MediaType = (int)item.MediaType,
            TmdbId = item.TmdbId,
            ImdbId = item.ImdbId,
            ReleaseYear = item.ReleaseYear,
            PosterUrl = item.PosterUrl
        };

        if (item.ImdbRating.HasValue)
            model.ImdbRating = item.ImdbRating.Value;

        if (item.ImdbVotes.HasValue)
            model.ImdbVotes = item.ImdbVotes.Value;

        if (item.BackdropUrl is not null)
            model.BackdropUrl = item.BackdropUrl;

        if (item.RuntimeMinutes.HasValue)
            model.RuntimeMinutes = item.RuntimeMinutes.Value;

        if (item.Director is not null)
            model.Director = item.Director;

        if (item.SeriesType.HasValue)
            model.SeriesType = (int)item.SeriesType.Value;

        if (item.TotalSeasons.HasValue)
            model.TotalSeasons = item.TotalSeasons.Value;

        model.PlatformIds.AddRange(item.PlatformIds.Select(id => id.ToString()));
        model.MoodIds.AddRange(item.MoodIds.Select(id => id.ToString()));
        model.GenreIds.AddRange(item.GenreIds.Select(id => id.ToString()));
        model.PlatformLinks.AddRange(item.PlatformLinks.Select(MapToPlatformLink));

        foreach (var (lang, payload) in item.Translations)
        {
            model.Translations.Add(lang, MapToLocalizedMediaPayload(payload));
        }

        return model;
    }

    private static StandaloneEpisodeGrpcModel MapToGrpcModel(ShuffleStandaloneEpisodeResponse item)
    {
        var model = new StandaloneEpisodeGrpcModel
        {
            EpisodeId = item.EpisodeId.ToString(),
            SeriesId = item.SeriesId.ToString(),
            SeasonId = item.SeasonId.ToString(),
            SeasonNumber = item.SeasonNumber,
            EpisodeNumber = item.EpisodeNumber,
            IsStandalone = item.IsStandalone,
            SeriesPosterUrl = item.SeriesPosterUrl,
            ReleaseYear = item.ReleaseYear
        };

        if (item.RuntimeMinutes.HasValue)
            model.RuntimeMinutes = item.RuntimeMinutes.Value;

        if (item.ImdbRating.HasValue)
            model.ImdbRating = item.ImdbRating.Value;

        if (item.ThumbnailUrl is not null)
            model.ThumbnailUrl = item.ThumbnailUrl;

        model.PlatformIds.AddRange(item.PlatformIds.Select(id => id.ToString()));
        model.MoodIds.AddRange(item.MoodIds.Select(id => id.ToString()));
        model.GenreIds.AddRange(item.GenreIds.Select(id => id.ToString()));
        model.PlatformLinks.AddRange(item.PlatformLinks.Select(MapToPlatformLink));

        foreach (var (lang, payload) in item.Translations)
        {
            var localized = new LocalizedEpisodePayload { Title = payload.Title };
            if (payload.Overview is not null)
                localized.Overview = payload.Overview;

            model.Translations.Add(lang, localized);
        }

        foreach (var (lang, payload) in item.SeriesTranslations)
        {
            model.SeriesTranslations.Add(lang, MapToLocalizedMediaPayload(payload));
        }

        return model;
    }

    private static PlatformDeepLinkGrpcModel MapToPlatformLink(ShufflePlatformLinkDto link)
    {
        var model = new PlatformDeepLinkGrpcModel
        {
            PlatformId = link.PlatformId.ToString(),
            PlatformName = link.PlatformName,
            PlatformSlug = link.PlatformSlug,
            LogoUrl = link.LogoUrl,
            WebUrl = link.WebUrl
        };

        if (link.AppSchemeUrl is not null)
            model.AppSchemeUrl = link.AppSchemeUrl;

        return model;
    }

    private static LocalizedMediaPayload MapToLocalizedMediaPayload(LocalizedMediaPayloadDto payload)
    {
        var model = new LocalizedMediaPayload
        {
            Title = payload.Title,
            Overview = payload.Overview
        };

        if (payload.Tagline is not null)
            model.Tagline = payload.Tagline;

        return model;
    }
}
