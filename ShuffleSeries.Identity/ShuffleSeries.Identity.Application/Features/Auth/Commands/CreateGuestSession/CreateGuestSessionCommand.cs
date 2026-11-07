using MediatR;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;

namespace ShuffleSeries.Identity.Application.Features.Auth.Commands.CreateGuestSession;

public sealed record CreateGuestSessionCommand(string? IpAddress = null) : IRequest<AuthResponse>;
