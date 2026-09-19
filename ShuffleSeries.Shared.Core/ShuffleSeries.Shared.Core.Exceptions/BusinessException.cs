using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public class BusinessException : CustomException
{
    public BusinessException(string code, string message)
        : base(message, HttpStatusCode.UnprocessableEntity, code, "Business Rule Violation")
    {
    }

    public BusinessException(string message)
        : base(message, HttpStatusCode.UnprocessableEntity, "BUSINESS_RULE_VIOLATION", "Business Rule Violation")
    {
    }

    public BusinessException()
        : base("A business rule violation occurred.", HttpStatusCode.UnprocessableEntity, "BUSINESS_RULE_VIOLATION",
            "Business Rule Violation")
    {
    }
}