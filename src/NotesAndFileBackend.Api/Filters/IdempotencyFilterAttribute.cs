using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace NotesAndFileBackend.Api.Filters;

public class IdempotencyFilterAttribute : IAsyncActionFilter
{
    private readonly IMemoryCache _cache;
    private const string IdempotencyHeader = "Idempotency-Key";

    public IdempotencyFilterAttribute(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeader, out var idempotencyKey))
        {
            var key = idempotencyKey.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                context.Result = new BadRequestObjectResult(new { error = "Idempotency-Key header is empty." });
                return;
            }

            var cacheKey = $"Idempotency_{key}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                context.Result = new ConflictObjectResult(new { error = new { code = "CONFLICT", message = "A request with this Idempotency-Key has already been processed or is currently processing." } });
                return;
            }

            // Set the cache key immediately to prevent race conditions from duplicate parallel requests
            // Cache it for 24 hours
            _cache.Set(cacheKey, true, TimeSpan.FromHours(24));
        }

        await next();
    }
}
