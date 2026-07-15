using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;

public sealed class DeleteSeriesCommandValidator : AbstractValidator<DeleteSeriesCommand>
{
    public DeleteSeriesCommandValidator()
    {
        RuleFor(x=>x.Id)
            .NotEmpty().WithMessage("Series Id is required.");
    }
}