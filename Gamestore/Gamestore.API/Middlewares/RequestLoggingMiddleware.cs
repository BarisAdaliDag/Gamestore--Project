using System.Diagnostics;
using System.Text;

namespace Gamestore.API.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);

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

            _logger.LogInformation(
                "HTTP {Method} {Url} | IP: {IP} | Status: {StatusCode} | Duration: {Duration}ms | Request: {RequestBody} | Response: {ResponseBody}",
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                TruncateBody(requestBody),
                TruncateBody(responseBody));
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

    private static string TruncateBody(string body)
    {
        const int MaxLength = 500;

        var truncatedBody = body.Length <= MaxLength
            ? body
            : body[..MaxLength] + "...[truncated]";

        return string.IsNullOrEmpty(body)
            ? "[empty]"
            : truncatedBody;
    }
}
