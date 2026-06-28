namespace ShuffleSeries.Shared.Core.Exceptions;

public class BusinessException : Exception
{
    public string Code { get; set; }
    
    public BusinessException(string code, string message) : base(message)
    {
        Code = code;
    }
    
    public BusinessException(string message) : base(message)
    {
    }
    
    public BusinessException()
    {
    }
}