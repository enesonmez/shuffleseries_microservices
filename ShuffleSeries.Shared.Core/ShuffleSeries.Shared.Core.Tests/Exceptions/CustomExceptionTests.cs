using System.Net;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Shared.Core.Tests.Exceptions;

public class CustomExceptionTests
{
    [Fact]
    public void BadRequestException_ShouldHaveCorrectDefaults()
    {
        var ex = new BadRequestException("Invalid input", "CUSTOM_CODE");
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Code.Should().Be("CUSTOM_CODE");
        ex.Message.Should().Be("Invalid input");

        var exDefault = new BadRequestException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exDefault.Code.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public void NotFoundException_ShouldHaveCorrectDefaults()
    {
        var ex = new NotFoundException("Resource not found");
        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Message.Should().Be("Resource not found");

        var guid = Guid.NewGuid();
        var ex2 = new NotFoundException("Series", guid);
        ex2.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex2.Code.Should().Be("SERIES_NOT_FOUND");
        ex2.Message.Should().Contain(guid.ToString());

        var exDefault = new NotFoundException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exDefault.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void ConflictException_ShouldHaveCorrectDefaults()
    {
        var ex = new ConflictException("Already exists", "CUSTOM_CONFLICT");
        ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Code.Should().Be("CUSTOM_CONFLICT");
        ex.Message.Should().Be("Already exists");

        var exDefault = new ConflictException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.Conflict);
        exDefault.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public void UnauthorizedException_ShouldHaveCorrectDefaults()
    {
        var ex = new UnauthorizedException("User not authenticated");
        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Message.Should().Be("User not authenticated");

        var exDefault = new UnauthorizedException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void ForbiddenException_ShouldHaveCorrectDefaults()
    {
        var ex = new ForbiddenException("Access forbidden");
        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Message.Should().Be("Access forbidden");

        var exDefault = new ForbiddenException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public void InternalServerException_ShouldHaveCorrectDefaults()
    {
        var inner = new Exception("Inner DB error");
        var ex = new InternalServerException("Internal error", "CUSTOM_INTERNAL", inner);
        ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Code.Should().Be("CUSTOM_INTERNAL");
        ex.Message.Should().Be("Internal error");
        ex.InnerException.Should().Be(inner);

        var exDefault = new InternalServerException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exDefault.Code.Should().Be("INTERNAL_SERVER_ERROR");
    }

    [Fact]
    public void BusinessException_ShouldStoreCodeAndMessage()
    {
        var ex = new BusinessException("SERIES_LIMIT_EXCEEDED", "Cannot add more series.");
        ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ex.Code.Should().Be("SERIES_LIMIT_EXCEEDED");
        ex.Message.Should().Be("Cannot add more series.");

        var exMsg = new BusinessException("Single message");
        exMsg.Code.Should().Be("BUSINESS_RULE_VIOLATION");
        exMsg.Message.Should().Be("Single message");

        var exDefault = new BusinessException();
        exDefault.Code.Should().Be("BUSINESS_RULE_VIOLATION");
        exDefault.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public void ValidationException_ShouldStoreErrorsProperly()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Title"] = new[] { "Title is required" },
            ["Year"] = new[] { "Year must be positive" }
        };

        var ex = new ValidationException(errors);
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Errors.Should().BeEquivalentTo(errors);

        var exDefault = new ValidationException();
        exDefault.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exDefault.Errors.Should().BeEmpty();

        var exSingle = new ValidationException("Title", "Title is required");
        exSingle.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exSingle.Errors.Should().ContainKey("Title");
        exSingle.Errors["Title"].Should().Contain("Title is required");
    }
}
