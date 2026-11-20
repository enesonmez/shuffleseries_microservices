using FluentValidation;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;

public sealed class CreateGuestSessionCommandValidator : AbstractValidator<CreateGuestSessionCommand>
{
    public CreateGuestSessionCommandValidator()
    {
        RuleFor(x => x.IpAddress)
            .MaximumLength(45)
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress));
    }
}
