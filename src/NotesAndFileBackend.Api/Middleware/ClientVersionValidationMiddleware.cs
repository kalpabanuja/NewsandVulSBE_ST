using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace NotesAndFileBackend.Api.Middleware;

public class ClientVersionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _minimumVersion = "1.0.0"; // Could be loaded from config in production

    public ClientVersionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-App-Version", out var appVersion))
        {
            if (Version.TryParse(appVersion, out var clientVersion) && 
                Version.TryParse(_minimumVersion, out var minVersion))
            {
                if (clientVersion < minVersion)
                {
                    context.Response.StatusCode = 426; // 426 Upgrade Required
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new 
                    {
                        error = "upgrade_required",
                        message = "Your app version is too old. Please update the app to continue using this service."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
