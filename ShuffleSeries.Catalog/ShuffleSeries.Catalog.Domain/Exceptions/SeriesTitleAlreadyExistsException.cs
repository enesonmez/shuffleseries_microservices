using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class SeriesTitleAlreadyExistsException : BusinessException
{
    private new const string Code = "SERIES_TITLE_ALREADY_EXISTS";

    public SeriesTitleAlreadyExistsException(string title) : base(Code,
        $"A series with the title '{title}' already exists.")
    {
    }
}