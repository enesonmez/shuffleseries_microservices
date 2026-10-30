using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Moods.AddMediaMood;

public sealed class AddMediaMoodCommandHandler : IRequestHandler<AddMediaMoodCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IMoodRepository _moodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddMediaMoodCommandHandler(
        IMediaRepository mediaRepository,
        IMoodRepository moodRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _moodRepository = moodRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddMediaMoodCommand request, CancellationToken cancellationToken)
    {
        var media = await _mediaRepository.GetByIdWithMoodsAsync(request.MediaId, cancellationToken)
            ?? throw new MediaNotFoundException(request.MediaId);

        _ = await _moodRepository.GetByIdAsync(request.MoodId, cancellationToken)
            ?? throw new MoodNotFoundException(request.MoodId);

        media.AddMood(request.MoodId);

        _mediaRepository.Update(media);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
