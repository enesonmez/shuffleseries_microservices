using MediatR;
using ShuffleSeries.Identity.Application.Features.Auth.Responses;

namespace ShuffleSeries.Identity.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResponse>;
