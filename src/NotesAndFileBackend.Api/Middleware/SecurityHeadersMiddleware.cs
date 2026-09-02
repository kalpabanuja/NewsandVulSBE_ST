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
        bool isIframeEndpoint = context.Request.Path.StartsWithSegments("/api/v1/public/Notes") && 
                                (context.Request.Path.Value?.EndsWith("/iframe", StringComparison.OrdinalIgnoreCase) == true);

        // Add Security Headers to all responses
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        if (!isIframeEndpoint)
        {
            context.Response.Headers.Append("X-Frame-Options", "DENY");
        }
        
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // The dashboard page (/) uses inline <script> and <style> tags, so it needs a relaxed CSP.
        // All other routes (API, health checks, etc.) get the strict policy.
        string csp;
        if (isIframeEndpoint)
        {
            // Controller sets its own strict CSP for the sandboxed iframe, avoid conflicts.
            csp = ""; 
        }
        else if (context.Request.Path == "/" || context.Request.Path.StartsWithSegments("/api/v1/public"))
        {
            csp = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; connect-src 'self'; frame-ancestors 'none';";
        }
        else
        {
            csp = "default-src 'self'; frame-ancestors 'none';";
        }

        if (!string.IsNullOrEmpty(csp))
        {
            context.Response.Headers.Append("Content-Security-Policy", csp);
        }

        // Strict-Transport-Security (HSTS) is typically handled by app.UseHsts(), but we can enforce it strictly here.
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        await _next(context);
    }
}
