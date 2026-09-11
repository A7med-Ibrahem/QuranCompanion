namespace QuranCompanion.Api.Common;

/// <summary>Uniform envelope for every API response so the frontend has one shape to handle.</summary>
public record ApiResponse<T>(bool Success, T? Data, ApiError? Error)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null);
    public static ApiResponse<T> Fail(string message, string code, IDictionary<string, string[]>? details = null)
        => new(false, default, new ApiError(code, message, details));
}

public record ApiError(string Code, string Message, IDictionary<string, string[]>? Details = null);
