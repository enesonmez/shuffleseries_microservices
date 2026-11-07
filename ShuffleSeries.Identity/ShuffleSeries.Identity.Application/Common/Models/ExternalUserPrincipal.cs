namespace ShuffleSeries.Identity.Application.Common.Models;

public sealed record ExternalUserPrincipal(
    string Provider,
    string SubjectId,
    string? Email,
    string? Name
);
