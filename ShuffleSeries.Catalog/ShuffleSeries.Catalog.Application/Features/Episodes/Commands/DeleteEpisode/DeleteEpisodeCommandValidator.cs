using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Episodes.Commands.DeleteEpisode;

public sealed class DeleteEpisodeCommandValidator : AbstractValidator<DeleteEpisodeCommand>
{
    public DeleteEpisodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Episode ID is required.");
    }
}
