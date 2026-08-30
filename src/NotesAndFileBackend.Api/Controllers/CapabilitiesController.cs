using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/app/capabilities")]
[AllowAnonymous]
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
