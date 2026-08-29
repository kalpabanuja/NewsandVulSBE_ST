using Microsoft.Extensions.Primitives;

namespace NotesAndFileBackend.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = GetCorrelationId(context);
        AddCorrelationIdToResponse(context, correlationId);

        using (logger.BeginScope("{CorrelationId}", correlationId))
        {
            return _next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString();
    }

    private static void AddCorrelationIdToResponse(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = new[] { correlationId };
            return Task.CompletedTask;
        });
    }
}
