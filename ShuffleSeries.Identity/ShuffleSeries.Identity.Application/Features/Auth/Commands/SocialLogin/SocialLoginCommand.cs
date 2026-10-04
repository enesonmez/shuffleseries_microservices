using FluentValidation;
using MediatR;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Attributes;
using ShuffleSeries.Shared.Core.Domain.Constants;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.SocialLogin;

public record SocialLoginCommand(
    string Provider,
    [property: MaskSensitiveData] string IdToken,
    [property: MaskSensitiveData] string? AppleRefreshToken = null,
    string? IpAddress = null
) : IRequest<AuthResponse>;

public class SocialLoginCommandValidator : AbstractValidator<SocialLoginCommand>
{
    public SocialLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .Must(p => p.Equals("Google", StringComparison.OrdinalIgnoreCase) || p.Equals("Apple", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only 'Google' and 'Apple' providers are supported.");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required.");
    }
}

internal sealed class SocialLoginCommandHandler : IRequestHandler<SocialLoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExternalAuthService _externalAuthService;
    private readonly ITokenService _tokenService;
    private readonly IPermissionResolver _permissionResolver;
    private readonly TimeProvider _timeProvider;

    public SocialLoginCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IExternalAuthService externalAuthService,
        ITokenService tokenService,
        IPermissionResolver permissionResolver,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _externalAuthService = externalAuthService;
        _tokenService = tokenService;
        _permissionResolver = permissionResolver;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
    {
        var externalPrincipal = await _externalAuthService.VerifyTokenAsync(request.Provider, request.IdToken, cancellationToken);
        if (externalPrincipal is null)
        {
            throw new InvalidExternalTokenException(request.Provider);
        }

        var user = await _userRepository.GetByLoginAsync(request.Provider, externalPrincipal.SubjectId, cancellationToken);

        if (user is null)
        {
            // Check if account with same email already exists
            if (!string.IsNullOrWhiteSpace(externalPrincipal.Email))
            {
                user = await _userRepository.GetByEmailAsync(externalPrincipal.Email.Trim().ToLowerInvariant(), cancellationToken);
            }

            if (user is not null)
            {
                // Account linking
                user.AddLogin(request.Provider, externalPrincipal.SubjectId, externalPrincipal.Email, request.AppleRefreshToken);
            }
            else
            {
                // Create brand new social user
                var fallbackEmail = !string.IsNullOrWhiteSpace(externalPrincipal.Email)
                    ? externalPrincipal.Email
                    : $"{request.Provider.ToLowerInvariant()}_{externalPrincipal.SubjectId}@shuffleseries.internal";

                user = User.CreateSocial(fallbackEmail, request.Provider, externalPrincipal.SubjectId);

                var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
                                  ?? await _roleRepository.GetByNameAsync(SystemRoles.Standard, cancellationToken);

                if (defaultRole is not null)
                {
                    user.AssignRole(defaultRole.Id);
                }

                _userRepository.Add(user);
            }
        }

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);
        var expiresAt = _timeProvider.GetUtcNow().AddDays(14).UtcDateTime;
        user.AddRefreshToken(hashedRefreshToken, expiresAt, request.IpAddress);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (roles, permissions) = await _permissionResolver.ResolveEffectivePermissionsAsync(user, cancellationToken);
        var tokenResponse = await _tokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email,
            user.IsGuest,
            roles,
            permissions,
            tokenResponse.AccessToken,
            rawRefreshToken,
            tokenResponse.ExpiresIn
        );
    }
}
