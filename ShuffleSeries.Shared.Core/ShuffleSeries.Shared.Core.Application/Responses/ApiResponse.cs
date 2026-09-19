namespace ShuffleSeries.Shared.Core.Application.Responses;

public class ApiResponse<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public string? Message { get; }
    public IReadOnlyCollection<string>? Errors { get; }

    public ApiResponse(bool success, T? data, string? message = null, IReadOnlyCollection<string>? errors = null)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
        => new(true, data, message);

    public static ApiResponse<T> FailureResult(string message, IReadOnlyCollection<string>? errors = null)
        => new(false, default, message, errors);
}

public class ApiResponse : ApiResponse<object?>
{
    public ApiResponse(bool success, string? message = null, IReadOnlyCollection<string>? errors = null)
        : base(success, null, message, errors)
    {
    }

    public static ApiResponse SuccessResult(string? message = null)
        => new(true, message);

    public new static ApiResponse FailureResult(string message, IReadOnlyCollection<string>? errors = null)
        => new(false, message, errors);
}
