using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

/// <summary>
/// İstemci kaynaklı geçersiz parametre, gövde veya sözleşme ihlallerinde fırlatılan genel HTTP 400 Bad Request istisnası.
/// </summary>
public class BadRequestException : CustomException
{
    public BadRequestException(string message, string? code = "BAD_REQUEST")
        : base(message, HttpStatusCode.BadRequest, code, "Bad Request")
    {
    }

    public BadRequestException()
        : base("A bad request error occurred.", HttpStatusCode.BadRequest, "BAD_REQUEST", "Bad Request")
    {
    }
}
