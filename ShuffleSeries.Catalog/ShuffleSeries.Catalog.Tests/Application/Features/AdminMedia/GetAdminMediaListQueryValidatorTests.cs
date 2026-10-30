using FluentValidation.TestHelper;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Queries.GetAdminMediaList;

namespace ShuffleSeries.Catalog.Tests.Application.Features.AdminMedia;

public class GetAdminMediaListQueryValidatorTests
{
    private readonly GetAdminMediaListQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ShouldNotHaveAnyValidationErrors()
    {
        var query = new GetAdminMediaListQuery(
            search: "Breaking Bad",
            pageNumber: 1,
            pageSize: 20);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrWhitespaceSearch_ShouldNotHaveValidationErrors(string? search)
    {
        var query = new GetAdminMediaListQuery(search: search);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Search);
    }

    [Fact]
    public void Validate_WithSearchLengthAtOrBelow100_ShouldBeValid()
    {
        var search = new string('a', 100);
        var query = new GetAdminMediaListQuery(search: search);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Search);
    }

    [Fact]
    public void Validate_WithSearchLengthExceeding100_ShouldHaveValidationError()
    {
        var search = new string('a', 101);
        var query = new GetAdminMediaListQuery(search: search);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Search)
            .WithErrorMessage("Search term cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithNullByteInSearch_ShouldHaveValidationError()
    {
        var query = new GetAdminMediaListQuery(search: "Walter\0White");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Search)
            .WithErrorMessage("Search term contains invalid null byte characters.");
    }

    [Theory]
    [InlineData("createdAt")]
    [InlineData("CREATEDAT")]
    [InlineData("imdbRating")]
    [InlineData("releaseYear")]
    [InlineData("title")]
    [InlineData(null)]
    public void Validate_WithAllowedSortByField_ShouldNotHaveValidationError(string? sortBy)
    {
        var query = new GetAdminMediaListQuery(sortBy: sortBy);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
    }

    [Theory]
    [InlineData("invalid_field")]
    [InlineData("DROP TABLE")]
    [InlineData("1; SELECT * FROM")]
    public void Validate_WithInvalidSortByField_ShouldHaveValidationError(string invalidSortBy)
    {
        var query = new GetAdminMediaListQuery(sortBy: invalidSortBy);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }
}
