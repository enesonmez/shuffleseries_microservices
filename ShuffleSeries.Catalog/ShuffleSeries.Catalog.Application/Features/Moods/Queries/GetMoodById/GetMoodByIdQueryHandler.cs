using MediatR;
using ShuffleSeries.Catalog.Application.Common.Extensions;
using ShuffleSeries.Catalog.Application.Features.Moods.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Moods.Queries.GetMoodById;

public sealed class GetMoodByIdQueryHandler : IRequestHandler<GetMoodByIdQuery, MoodResponse>
{
    private readonly IMoodRepository _moodRepository;
    private readonly ILanguageContext _languageContext;

    public GetMoodByIdQueryHandler(
        IMoodRepository moodRepository,
        ILanguageContext languageContext)
    {
        _moodRepository = moodRepository;
        _languageContext = languageContext;
    }

    public async Task<MoodResponse> Handle(GetMoodByIdQuery request, CancellationToken cancellationToken)
    {
        var mood = await _moodRepository.GetByIdForDetailAsync(request.Id, cancellationToken)
            ?? throw new MoodNotFoundException(request.Id);

        var targetLanguage = request.Language ?? _languageContext.CurrentLanguage;
        var localized = mood.GetLocalizedTranslation(targetLanguage);

        var translations = mood.Translations
            .Select(t => new MoodTranslationResponse(t.LanguageCode.Value, t.Name, t.Description, t.VibeTags))
            .ToList();

        return new MoodResponse(
            mood.Id,
            mood.Code,
            mood.AmbientColorHex,
            mood.IconUrl,
            mood.DisplayOrder,
            mood.IsActive,
            localized?.Name ?? mood.Code,
            localized?.Description ?? string.Empty,
            localized?.VibeTags,
            translations);
    }
}
