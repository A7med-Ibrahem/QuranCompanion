using System.Net;
using System.Text.Json;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Exceptions;

namespace QuranCompanion.Api.Middleware;

/// <summary>
/// Central place that turns every exception into the clean, user-friendly
/// JSON shape required by spec section 23 - raw stack traces never reach the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationApiException vex)
        {
            await WriteAsync(context, vex.StatusCode, ApiResponse<object>.Fail(vex.Message, vex.ErrorCode, vex.Errors));
        }
        catch (ApiException apiEx)
        {
            await WriteAsync(context, apiEx.StatusCode, ApiResponse<object>.Fail(apiEx.Message, apiEx.ErrorCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteAsync(context, (int)HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail("Something went wrong on our end. Please try again.", "internal_error"));
        }
    }

    private static Task WriteAsync(HttpContext context, int statusCode, ApiResponse<object> body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(json);
    }
}
