using Microsoft.AspNetCore.Http;

namespace NotesAndFileBackend.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add Security Headers to all responses
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Basic Content-Security-Policy (adjust as needed for specific endpoints or UI integration)
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");

        // Strict-Transport-Security (HSTS) is typically handled by app.UseHsts(), but we can enforce it strictly here.
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        await _next(context);
    }
}
