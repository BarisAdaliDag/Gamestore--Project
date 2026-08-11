using System.Net;
using System.Text.Json;
using Gamestore.BLL.Exceptions;

namespace Gamestore.API.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogExceptionDetails(ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private void LogExceptionDetails(Exception exception)
    {
        // ForbiddenException is an expected authorization rejection, not a server error
        if (exception is ForbiddenException)
        {
            _logger.LogWarning(
                "Forbidden: {Message}",
                exception.Message);
            return;
        }

        _logger.LogError(
            exception,
            "Exception Type: {ExceptionType} | Message: {Message} | InnerException: {InnerException}",
            exception.GetType().FullName,
            exception.Message,
            GetInnerExceptions(exception));
    }

    private static string GetInnerExceptions(Exception exception)
    {
        var innerMessages = new List<string>();
        var inner = exception.InnerException;

        while (inner != null)
        {
            innerMessages.Add($"[{inner.GetType().Name}] {inner.Message}");
            inner = inner.InnerException;
        }

        return innerMessages.Count > 0
            ? string.Join(" -> ", innerMessages)
            : "None";
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message),
            EntityAlreadyExistsException => (HttpStatusCode.Conflict, exception.Message),
            EntityInUseException => (HttpStatusCode.Conflict, exception.Message),
            OutOfStockException => (HttpStatusCode.Conflict, exception.Message),
            ValidationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred."),
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            statusCode = (int)statusCode,
            error = exception is OutOfStockException ? "OUT_OF_STOCK" : statusCode.ToString(),
            message,
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
