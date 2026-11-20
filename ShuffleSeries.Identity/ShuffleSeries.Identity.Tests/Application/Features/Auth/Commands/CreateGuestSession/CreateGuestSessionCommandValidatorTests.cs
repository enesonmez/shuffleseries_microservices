using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.CreateGuestSession;

public class CreateGuestSessionCommandValidatorTests
{
    private readonly CreateGuestSessionCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new CreateGuestSessionCommand("127.0.0.1");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Ip_IsNull()
    {
        var command = new CreateGuestSessionCommand(null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Ip_Exceeds_45_Chars()
    {
        var command = new CreateGuestSessionCommand(new string('a', 46));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IpAddress);
    }
}
