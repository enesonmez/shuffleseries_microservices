using System.Net;
using ShuffleSeries.Identity.Domain.Exceptions;

namespace ShuffleSeries.Identity.Tests.Domain;

public class IdentityDomainExceptionTests
{
    [Fact]
    public void EmailAlreadyInUseException_ShouldHaveCorrectDefaults()
    {
        var ex = new EmailAlreadyInUseException("test@example.com");

        ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Code.Should().Be("USER_ALREADY_EXISTS");
        ex.Message.Should().Be("A user with email 'test@example.com' already exists.");
    }

    [Fact]
    public void InvalidCredentialsException_ShouldHaveCorrectDefaults()
    {
        var ex = new InvalidCredentialsException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("INVALID_CREDENTIALS");
        ex.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public void InvalidRefreshTokenException_ShouldHaveCorrectDefaults()
    {
        var ex = new InvalidRefreshTokenException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("INVALID_REFRESH_TOKEN");
        ex.Message.Should().Be("Invalid refresh token.");
    }

    [Fact]
    public void RefreshTokenExpiredException_ShouldHaveCorrectDefaults()
    {
        var ex = new RefreshTokenExpiredException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("REFRESH_TOKEN_EXPIRED");
        ex.Message.Should().Be("Refresh token has expired.");
    }

    [Fact]
    public void TokenCompromisedException_ShouldHaveCorrectDefaults()
    {
        var ex = new TokenCompromisedException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("TOKEN_REUSE_DETECTED");
        ex.Message.Should().Be("Compromised token detected. All active sessions have been revoked.");
    }

    [Fact]
    public void TokenNotFoundException_ShouldHaveCorrectDefaults()
    {
        var ex = new TokenNotFoundException();

        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Code.Should().Be("TOKEN_NOT_FOUND");
        ex.Message.Should().Be("Token not found.");
    }

    [Fact]
    public void InvalidExternalTokenException_ShouldHaveCorrectDefaults()
    {
        var ex = new InvalidExternalTokenException("Google");

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("INVALID_EXTERNAL_TOKEN");
        ex.Message.Should().Be("Invalid Google authentication token.");
    }

    [Fact]
    public void GuestUserNotFoundException_ShouldHaveCorrectDefaults()
    {
        var ex = new GuestUserNotFoundException();

        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Code.Should().Be("GUEST_USER_NOT_FOUND");
        ex.Message.Should().Be("Guest user not found or is already registered.");
    }

    [Fact]
    public void TargetUserNotFoundException_ShouldHaveCorrectDefaults()
    {
        var ex = new TargetUserNotFoundException();

        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Code.Should().Be("TARGET_USER_NOT_FOUND");
        ex.Message.Should().Be("Target user account not found.");
    }

    [Fact]
    public void UserNotFoundException_ShouldHaveCorrectDefaults()
    {
        var ex = new UserNotFoundException();

        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Code.Should().Be("USER_NOT_FOUND");
        ex.Message.Should().Be("User not found.");

        var userId = Guid.NewGuid();
        var exWithId = new UserNotFoundException(userId);
        exWithId.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exWithId.Code.Should().Be("USER_NOT_FOUND");
        exWithId.Message.Should().Be($"User with ID '{userId}' was not found.");
    }

    [Fact]
    public void CannotMergeIntoDifferentUserException_ShouldHaveCorrectDefaults()
    {
        var ex = new CannotMergeIntoDifferentUserException();

        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Code.Should().Be("FORBIDDEN");
        ex.Message.Should().Be("Cannot merge guest account into another user's account.");
    }

    [Fact]
    public void UserAlreadyRegisteredException_ShouldHaveCorrectDefaults()
    {
        var ex = new UserAlreadyRegisteredException();

        ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ex.Code.Should().Be("USER_ALREADY_REGISTERED");
        ex.Message.Should().Be("User is already a registered user.");
    }

    [Fact]
    public void UnidentifiedUserTokenException_ShouldHaveCorrectDefaults()
    {
        var ex = new UnidentifiedUserTokenException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Code.Should().Be("UNAUTHORIZED");
        ex.Message.Should().Be("User ID could not be identified from token.");
    }
}
