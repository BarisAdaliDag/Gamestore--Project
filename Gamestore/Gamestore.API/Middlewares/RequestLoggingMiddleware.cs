using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace Gamestore.API.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);
        using var logScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["RequestId"] = context.TraceIdentifier,
            });

        var originalResponseBody = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            stopwatch.Stop();
            var responseBody = await ReadResponseBodyAsync(responseBodyStream);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);

            _logger.Log(
                GetLogLevel(context.Response.StatusCode),
                "HTTP {Method} {Url} | IP: {IP} | Status: {StatusCode} | Duration: {Duration}ms | Request: {RequestBody} | Response: {ResponseBody}",
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                PrepareBodyForLogging(requestBody),
                PrepareBodyForLogging(responseBody));
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBodyStream)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        responseBodyStream.Seek(0, SeekOrigin.Begin);

        return body;
    }

    private static LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= StatusCodes.Status500InternalServerError => LogLevel.Error,
            >= StatusCodes.Status400BadRequest => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }

    private static string PrepareBodyForLogging(string body)
    {
        const int MaxLength = 500;

        return string.IsNullOrEmpty(body)
            ? "[empty]"
            : TruncateBody(RedactSensitiveData(body), MaxLength);
    }

    private static string TruncateBody(string body, int maxLength)
    {
        return body.Length <= maxLength
            ? body
            : body[..maxLength] + "...[truncated]";
    }

    private static string RedactSensitiveData(string body)
    {
        try
        {
            var json = JsonNode.Parse(body);
            RedactSensitiveProperties(json);
            return json?.ToJsonString() ?? body;
        }
        catch
        {
            return body;
        }
    }

    private static void RedactSensitiveProperties(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitiveProperty(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                }
                else
                {
                    RedactSensitiveProperties(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                RedactSensitiveProperties(item);
            }
        }
    }

    private static bool IsSensitiveProperty(string propertyName)
    {
        return propertyName.Equals("password", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("token", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("accessToken", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("refreshToken", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("secret", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("apiKey", StringComparison.OrdinalIgnoreCase);
    }
}
