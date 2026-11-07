using FluentValidation.TestHelper;
using ShuffleSeries.Identity.Application.Features.Auth.Commands.DeleteAccount;

namespace ShuffleSeries.Identity.Tests.Application.Features.Auth.Commands.DeleteAccount;

public class DeleteAccountCommandValidatorTests
{
    private readonly DeleteAccountCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new DeleteAccountCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_UserId_IsEmpty()
    {
        var command = new DeleteAccountCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
