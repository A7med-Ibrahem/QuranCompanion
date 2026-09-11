namespace QuranCompanion.Application.Common.Exceptions;

/// <summary>
/// Base for exceptions that should be translated into clean, user-friendly
/// API error responses instead of leaking raw stack traces (see spec section 23).
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public ApiException(string message, int statusCode = 400, string errorCode = "bad_request")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class ValidationApiException : ApiException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationApiException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", 422, "validation_error")
    {
        Errors = errors;
    }
}

public class NotFoundApiException : ApiException
{
    public NotFoundApiException(string message) : base(message, 404, "not_found") { }
}

public class UnauthorizedApiException : ApiException
{
    public UnauthorizedApiException(string message) : base(message, 401, "unauthorized") { }
}
