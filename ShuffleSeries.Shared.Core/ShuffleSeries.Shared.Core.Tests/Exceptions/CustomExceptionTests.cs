using System.Net;
using FluentAssertions;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Shared.Core.Tests.Exceptions;

public class CustomExceptionTests
{
    [Fact]
    public void BusinessException_ShouldHaveCorrectDefaults()
    {
        var ex = new BusinessException("CUSTOM_ERR", "Something business-related went wrong.");

        ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ex.Code.Should().Be("CUSTOM_ERR");
        ex.Message.Should().Be("Something business-related went wrong.");
        ex.Title.Should().Be("Business Rule Violation");
    }

    [Fact]
    public void NotFoundException_WithEntityAndKey_ShouldFormatMessageCorrectly()
    {
        var id = Guid.NewGuid();
        var ex = new NotFoundException("Movie", id);

        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Code.Should().Be("MOVIE_NOT_FOUND");
        ex.Message.Should().Be($"Entity 'Movie' with identifier '{id}' was not found.");
        ex.Title.Should().Be("Not Found");
    }

    [Fact]
    public void ValidationException_WithErrorsDictionary_ShouldStoreErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Title"] = ["Title is required.", "Title is too long."],
            ["Year"] = ["Year must be positive."]
        };

        var ex = new ValidationException(errors);

        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Code.Should().Be("VALIDATION_ERROR");
        ex.Title.Should().Be("Validation Error");
        ex.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ValidationException_WithPropertyAndMessage_ShouldStoreSingleError()
    {
        var ex = new ValidationException("Title", "Title is required.");

        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Errors.Should().ContainKey("Title");
        ex.Errors["Title"].Should().ContainSingle("Title is required.");
    }

    [Fact]
    public void ConflictException_ShouldHaveConflictStatus()
    {
        var ex = new ConflictException("Resource already exists.");

        ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Code.Should().Be("CONFLICT");
        ex.Title.Should().Be("Conflict");
        ex.Message.Should().Be("Resource already exists.");
    }

    [Fact]
    public void UnauthorizedException_ShouldHaveUnauthorizedStatus()
    {
        var ex = new UnauthorizedException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("UNAUTHORIZED");
        ex.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public void ForbiddenException_ShouldHaveForbiddenStatus()
    {
        var ex = new ForbiddenException();

        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Code.Should().Be("FORBIDDEN");
        ex.Title.Should().Be("Forbidden");
    }

    [Fact]
    public void InternalServerException_ShouldHaveInternalServerErrorStatus()
    {
        var inner = new InvalidOperationException("DB failed");
        var ex = new InternalServerException("Something crashed", "CRASH", inner);

        ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Code.Should().Be("CRASH");
        ex.Title.Should().Be("Internal Server Error");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void BadRequestException_ShouldHaveBadRequestStatus()
    {
        var ex = new BadRequestException("Invalid parameter value.", "INVALID_PARAM");

        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Code.Should().Be("INVALID_PARAM");
        ex.Title.Should().Be("Bad Request");
        ex.Message.Should().Be("Invalid parameter value.");
    }
}
