namespace ShuffleSeries.Shared.Core.Web.Helpers;

/// <summary>
/// RFC 7807 ProblemDetails yanıtı için durum kodu, başlık, detay ve hata kodu bilgilerini kapsüller.
/// </summary>
public sealed record ProblemDetailsDescriptor(
    int StatusCode,
    string Title,
    string Detail,
    string Code,
    string? Type = null,
    Action<Microsoft.AspNetCore.Mvc.ProblemDetails>? Configure = null);
