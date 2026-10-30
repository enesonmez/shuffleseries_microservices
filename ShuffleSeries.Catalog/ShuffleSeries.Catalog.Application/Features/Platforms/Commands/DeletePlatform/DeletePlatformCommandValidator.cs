using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Platforms.Commands.DeletePlatform;

public sealed class DeletePlatformCommandValidator : AbstractValidator<DeletePlatformCommand>
{
    public DeletePlatformCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Platform ID is required.");
    }
}
