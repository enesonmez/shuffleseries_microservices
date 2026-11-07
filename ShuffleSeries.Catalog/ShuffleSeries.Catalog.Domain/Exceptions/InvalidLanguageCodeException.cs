using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class InvalidLanguageCodeException : BadRequestException
{
    public InvalidLanguageCodeException(string languageCode)
        : base($"Language code '{languageCode}' is invalid. Supported codes are ISO 639-1 format (e.g. 'tr', 'en').", "INVALID_LANGUAGE_CODE")
    {
    }
}
