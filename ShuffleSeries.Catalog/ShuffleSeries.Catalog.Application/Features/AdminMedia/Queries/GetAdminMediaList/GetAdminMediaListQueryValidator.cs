using FluentValidation;

namespace ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;

public sealed class GetAdminMediaListQueryValidator : AbstractValidator<GetAdminMediaListQuery>
{
    public const int MaxSearchLength = 100;
    public const int MaxPageSize = 100;
    private static readonly string[] _allowedSortByFields = ["createdat", "imdbrating", "releaseyear", "title"];

    public GetAdminMediaListQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .WithMessage($"Search term cannot exceed {MaxSearchLength} characters.")
            .Must(s => !s.Contains('\0'))
            .WithMessage("Search term contains invalid null byte characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.SortBy)
            .Must(s => _allowedSortByFields.Contains(s!.Trim().ToLowerInvariant()))
            .WithMessage($"Invalid SortBy field. Allowed values: {string.Join(", ", _allowedSortByFields)}.")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Page size must be between 1 and {MaxPageSize}.");
    }
}
