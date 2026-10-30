using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.UpdateMovie;

public sealed class UpdateMovieCommandHandler : IRequestHandler<UpdateMovieCommand>
{
    private readonly IMovieRepository _movieRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMovieCommandHandler(
        IMovieRepository movieRepository,
        IUnitOfWork unitOfWork)
    {
        _movieRepository = movieRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new MovieNotFoundException(request.Id);

        if (await _movieRepository.ExistsByTmdbIdAsync(request.TmdbId, movie.Id, cancellationToken))
        {
            throw new DuplicateTmdbIdException(request.TmdbId);
        }

        if (await _movieRepository.ExistsByImdbIdAsync(request.ImdbId, movie.Id, cancellationToken))
        {
            throw new DuplicateImdbIdException(request.ImdbId);
        }

        movie.UpdateDetails(
            request.TmdbId,
            request.ImdbId,
            request.ReleaseYear,
            request.RuntimeMinutes,
            request.Director,
            request.ImdbRating,
            request.ImdbVotes,
            request.PosterUrl,
            request.BackdropUrl);

        _movieRepository.Update(movie);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
