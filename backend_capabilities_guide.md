# Backend Implementation Guide: App Capabilities & Versioning

The frontend MAUI client is now designed to support dynamic feature flags, API versioning, and graceful fallbacks. To fully utilize this architecture, you need to implement a capabilities endpoint and request validation on your ASP.NET Core backend.

Here is what you need to build on the backend to complete this integration.

---

## 1. The Capabilities Endpoint

You need a public or authenticated endpoint that the MAUI client can query on startup to understand what features the backend currently supports.

### **Endpoint Definition**
```http
GET /api/v1/app/capabilities
```

### **Expected JSON Response**
```json
{
  "apiVersion": "v1",
  "minimumClientVersion": "1.0.0",
  "features": {
    "commandGenerators": true,
    "publicShares": true,
    "noteRevisions": true,
    "offlineSync": false,
    "markdownImport": false
  }
}
```

### **C# Backend Implementation Example**
Create a simple controller to serve these flags. This is particularly useful if you want to enable/disable features in production without pushing an app update.

```csharp
[ApiController]
[Route("api/v1/app/[controller]")]
public class CapabilitiesController : ControllerBase
{
    private readonly IConfiguration _config;
    
    public CapabilitiesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult GetCapabilities()
    {
        var response = new
        {
            ApiVersion = "v1",
            // The lowest version of the MAUI app allowed to connect
            MinimumClientVersion = _config["AppConfig:MinimumClientVersion"] ?? "1.0.0",
            Features = new Dictionary<string, bool>
            {
                { "commandGenerators", true },
                { "publicShares", true },
                { "noteRevisions", true },
                { "offlineSync", false },
                { "markdownImport", false }
            }
        };

        return Ok(response);
    }
}
```

---

## 2. Version Header Validation (Optional but Recommended)

The MAUI client now sends an `X-App-Version` header with every authenticated API request. 
Example: `X-App-Version: 1.0.0.1`

You can use this header in the backend to:
1. **Force Updates:** Block requests from outdated, deprecated client versions.
2. **Analytics:** Log which versions of the app your users are running.
3. **Payload Adjustments:** Alter the JSON structure if a legacy client requests data.

### **C# Backend Middleware Example**
You can create a middleware or an Action Filter to enforce minimum version requirements globally.

```csharp
public class ClientVersionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _minimumVersion = "1.0.0"; // Load from config in production

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
```

---

## 3. Safe Schema Evolution (What this enables)

Because the MAUI client has been upgraded to safely handle unknown schema properties, you can now do the following on the backend **without breaking older installed apps**:

1. **Add new `BlockType` values to Notes:** 
   If you add a new block type (e.g. `Type = "Table"`) on the backend, old MAUI clients will parse it, realize they don't have a template for it, and gracefully render the new warning block: *"Unsupported content block. Please update the app to view this content."*
   
2. **Add new Command Generator Fields:**
   If you add a new input type (e.g. `Type = "date_picker"`) to the command generator JSON, old MAUI clients will render an *"Unsupported field"* label instead of throwing a fatal crash.

You do not need to do anything specific on the backend to enable this—just be aware that you can now freely evolve your JSON schemas and the frontend won't break!
