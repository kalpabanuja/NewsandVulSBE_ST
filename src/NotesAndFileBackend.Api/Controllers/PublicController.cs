using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using NotesAndFileBackend.Application.Interfaces;
using NotesAndFileBackend.Application.Services;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
[EnableRateLimiting("StrictPolicy")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;

    public PublicController(AppDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    [HttpGet("files/{token}")]
    public async Task<IActionResult> GetSharedFile(string token, CancellationToken ct)
    {
        var share = await _context.PublicFileShares
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.TokenHash == token && s.RevokedAt == null, ct);

        if (share == null || share.File.Status != "ACTIVE")
            return NotFound(new { error = new { message = "Share link is invalid or expired." } });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return NotFound(new { error = new { message = "Share link has expired." } });

        // Increment stats
        share.AccessCount++;
        share.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Stream the file directly through the API
        var stream = await _storageService.DownloadFileAsync(share.File.StoredFilename);
        
        return File(stream, share.File.MimeType, share.File.OriginalFilename);
    }

    [HttpGet("Notes/{token}")]
    public async Task<IActionResult> GetSharedNote(string token, [FromHeader(Name = "X-Share-Password")] string? headerPassword, [FromQuery(Name = "pwd")] string? queryPassword, CancellationToken ct)
    {
        var share = await _context.PublicNoteShares
            .Include(s => s.Note)
            .FirstOrDefaultAsync(s => s.TokenHash == token, ct);

        // Revoked link → 410 Gone
        if (share != null && share.RevokedAt != null)
            return GoneResponse(Request);

        if (share == null || share.Note.IsDeleted)
            return NotFound(new { error = new { message = "Share link is invalid." } });

        // Expired link → 410 Gone
        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return GoneResponse(Request);

        // Max views exceeded → 410 Gone
        if (share.MaxViews.HasValue && share.ViewCount >= share.MaxViews.Value)
            return GoneResponse(Request);

        // Password protection
        var password = headerPassword ?? queryPassword;
        if (!string.IsNullOrWhiteSpace(share.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(password) || !BCrypt.Net.BCrypt.Verify(password, share.PasswordHash))
            {
                if (Request.Headers["Accept"].ToString().Contains("text/html"))
                    return Content(BuildPasswordPromptHtml(), "text/html", Encoding.UTF8);

                return Unauthorized(new { error = new { message = "Invalid or missing password." } });
            }
        }

        // Update stats + emit audit event
        share.ViewCount++;
        share.LastAccessedAt = DateTime.UtcNow;
        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            EventType = "note.share_accessed",
            ResourceType = "note",
            ResourceId = share.NoteId.ToString()
        });
        await _context.SaveChangesAsync(ct);

        // Build DTO for API clients
        var contentJsonb = share.Note.ContentJsonb;
        JsonElement parsedContent;
        try { parsedContent = JsonSerializer.Deserialize<JsonElement>(contentJsonb); }
        catch { parsedContent = JsonSerializer.Deserialize<JsonElement>("{}"); }

        var dto = new NotesAndFileBackend.Api.DTOs.SharedNoteDto
        {
            Id = share.Note.Id,
            Title = share.Note.Title,
            Summary = share.Note.Summary,
            ToolName = share.Note.ToolName ?? string.Empty,
            ContentJsonb = parsedContent,
            UpdatedAt = share.Note.UpdatedAt
        };

        if (!share.AllowIndexing)
            Response.Headers.Append("X-Robots-Tag", "noindex, nofollow");

        // Browser → render proper HTML
        if (Request.Headers["Accept"].ToString().Contains("text/html"))
            return Content(BuildNoteHtml(share.Note.Title, share.Note.Summary, share.Note.UpdatedAt, contentJsonb), "text/html", Encoding.UTF8);

        // API client → return JSON
        return Ok(dto);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private IActionResult GoneResponse(HttpRequest request)
    {
        if (request.Headers["Accept"].ToString().Contains("text/html"))
        {
            var html = @"<!DOCTYPE html>
<html>
<head>
    <title>Link Expired</title>
    <style>
        body { font-family: system-ui, -apple-system, sans-serif; max-width: 500px; margin: 100px auto; text-align: center; color: #555; }
        h1 { color: #111; }
    </style>
</head>
<body>
    <h1>&#128279; Link Expired or Unavailable</h1>
    <p>This shared link has expired, been revoked, or is no longer available.</p>
</body>
</html>";
            Response.StatusCode = 410;
            return Content(html, "text/html", Encoding.UTF8);
        }

        return StatusCode(410, new { error = new { code = "GONE", message = "This share link has expired or is no longer available." } });
    }

    private static string BuildPasswordPromptHtml() => @"<!DOCTYPE html>
<html>
<head>
    <title>Password Protected Note</title>
    <style>
        body { font-family: system-ui, -apple-system, sans-serif; max-width: 400px; margin: 100px auto; text-align: center; color: #333; background: #f9fafb; }
        .card { background: white; padding: 2rem; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border: 1px solid #e5e7eb; }
        input { padding: 12px; font-size: 1rem; width: 100%; box-sizing: border-box; margin: 15px 0; border: 1px solid #d1d5db; border-radius: 6px; outline: none; }
        input:focus { border-color: #0284c7; box-shadow: 0 0 0 3px rgba(2,132,199,0.1); }
        button { padding: 12px; font-size: 1rem; background: #0ea5e9; color: white; border: none; border-radius: 6px; width: 100%; cursor: pointer; font-weight: 600; transition: background 0.2s; }
        button:hover { background: #0284c7; }
    </style>
</head>
<body>
    <div class='card'>
        <svg style='width:48px;height:48px;color:#0ea5e9;margin-bottom:10px;' fill='none' stroke='currentColor' viewBox='0 0 24 24'><path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'></path></svg>
        <h2 style='margin-top:0;'>Protected Note</h2>
        <p style='color:#666;font-size:0.9rem;'>This note is secured. Enter the password to unlock it.</p>
        <form method='GET'>
            <input type='password' name='pwd' placeholder='Enter password' required autofocus />
            <button type='submit'>Unlock Note</button>
        </form>
    </div>
</body>
</html>";

    private static string BuildNoteHtml(string title, string summary, DateTime? updatedAt, string contentJsonb)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine($"  <title>{Encode(title)}</title>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.AppendLine(@"  <style>
    *, *::before, *::after { box-sizing: border-box; }
    body { font-family: system-ui, -apple-system, sans-serif; line-height: 1.7; max-width: 800px; margin: 0 auto; padding: 2rem 1.5rem; color: #1f2937; background: #fff; }
    h1.note-title { font-size: 2rem; border-bottom: 2px solid #e5e7eb; padding-bottom: .5rem; margin-bottom: .5rem; }
    .note-summary { color: #6b7280; font-size: 1.1rem; margin-bottom: 2rem; font-style: italic; }
    h1,h2,h3,h4,h5 { color: #111827; margin-top: 1.5rem; }
    p { margin: .75rem 0; }
    ul.style-disc  { list-style-type: disc; }
    ul.style-circle{ list-style-type: circle; }
    ul.style-square{ list-style-type: square; }
    ul.style-dash  { list-style-type: none; padding-left: 1.5rem; }
    ul.style-dash li::before { content: '-'; display: inline-block; width: 1rem; margin-left: -1rem; }
    ol { list-style-type: decimal; }
    ul.checklist { list-style: none; padding-left: 0; }
    ul.checklist li { display: flex; align-items: center; gap: .5rem; }
    ul.checklist li input[type=checkbox] { cursor: default; }
    hr { border: none; border-top: 3px dashed #d1d5db; margin: 1.5rem 0; }
    hr.divider-singleLine { border: none; border-top: 1px solid #d1d5db; margin: 1.5rem 0; }
    hr.divider-dots { border: none; text-align: center; margin: 1.5rem 0; color: #9ca3af; }
    hr.divider-dots::after { content: '• • •'; }
    hr.divider-breakLines { border: none; border-top: 3px dashed #d1d5db; margin: 1.5rem 0; }
    hr.divider-doubleLine { border: none; border-top: 3px double #d1d5db; margin: 1.5rem 0; }
    hr.divider-space { border: none; margin: 2.5rem 0; }
    .code-block { position: relative; border-radius: 8px; margin: 1rem 0; overflow: hidden; }
    .code-block pre { margin: 0; padding: 1.25rem 1rem; overflow-x: auto; font-family: 'Fira Code', monospace, monospace; font-size: 0.9rem; white-space: pre; }
    .copy-btn { position: absolute; top: .5rem; right: .5rem; padding: .3rem .7rem; font-size: .75rem; background: rgba(255,255,255,.15); color: #fff; border: 1px solid rgba(255,255,255,.3); border-radius: 4px; cursor: pointer; }
    .copy-btn:hover { background: rgba(255,255,255,.3); }
    a { color: #2563eb; text-decoration: underline; }
    a:hover { color: #1d4ed8; }
    .file-card { display: flex; align-items: center; gap: 1rem; border: 1px solid #e5e7eb; border-radius: 8px; padding: 1rem; margin: .75rem 0; background: #f9fafb; }
    .file-card .file-info { flex: 1; }
    .file-card .file-name { font-weight: 600; }
    .file-card .file-size { color: #6b7280; font-size: .85rem; }
    .download-btn { padding: .4rem .9rem; background: #2563eb; color: white; border-radius: 6px; text-decoration: none; font-size: .9rem; }
    .meta-footer { margin-top: 3rem; font-size: .85rem; color: #9ca3af; border-top: 1px solid #e5e7eb; padding-top: 1rem; }
  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"  <h1 class='note-title'>{Encode(title)}</h1>");
        if (!string.IsNullOrWhiteSpace(summary))
            sb.AppendLine($"  <p class='note-summary'>{Encode(summary)}</p>");

        // Render blocks
        try
        {
            using var doc = JsonDocument.Parse(contentJsonb);
            var root = doc.RootElement;
            if (root.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in blocks.EnumerateArray())
                    sb.AppendLine(RenderBlock(block));
            }
        }
        catch
        {
            // Fallback: show plain text
            sb.AppendLine($"  <pre>{Encode(contentJsonb)}</pre>");
        }

        var updated = updatedAt.HasValue ? updatedAt.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty;
        sb.AppendLine($"  <div class='meta-footer'>Shared securely via ThreatIntel &bull; Last updated: {Encode(updated)}</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string RenderBlock(JsonElement block)
    {
        var blockType = block.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

        return blockType.ToLowerInvariant() switch
        {
            "heading" => RenderHeading(block),
            "paragraph" => RenderParagraph(block),
            "bulletlist" => RenderBulletList(block),
            "numberedlist" => RenderNumberedList(block),
            "checklist" => RenderChecklist(block),
            "divider" => RenderDivider(block),
            "link" => RenderLink(block),
            "code" => RenderCode(block),
            "displayattachment" => "<!-- display attachment (inline preview) -->",
            "downloadattachment" => RenderDownloadAttachment(block),
            "commandgenerator" => RenderCommandGenerator(block),
            "copycard" => RenderCopyCard(block),
            _ => $"<!-- unsupported block type: {Encode(blockType)} -->"
        };
    }

    private static string RenderHeading(JsonElement block)
    {
        var level = block.TryGetProperty("level", out var lp) && lp.TryGetInt32(out var l) ? Math.Clamp(l, 1, 5) : 2;
        var text = block.TryGetProperty("text", out var tp) ? tp.GetString() ?? "" : "";
        return $"  <h{level}>{Encode(text)}</h{level}>";
    }

    private static string RenderParagraph(JsonElement block)
    {
        var text = block.TryGetProperty("text", out var tp) ? tp.GetString() ?? "" : "";
        return $"  <p>{Encode(text)}</p>";
    }

    private static string RenderBulletList(JsonElement block)
    {
        var style = block.TryGetProperty("style", out var sp) ? sp.GetString() ?? "disc" : "disc";
        var allowedStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "disc", "circle", "square", "dash" };
        if (!allowedStyles.Contains(style)) style = "disc";
        var sb = new StringBuilder($"  <ul class='style-{Encode(style)}'>\n");
        if (block.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            foreach (var item in items.EnumerateArray())
            {
                var text = "";
                if (item.ValueKind == JsonValueKind.String) text = item.GetString() ?? "";
                else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("text", out var tp)) text = tp.GetString() ?? "";
                
                sb.AppendLine($"    <li>{Encode(text)}</li>");
            }
        sb.Append("  </ul>");
        return sb.ToString();
    }

    private static string RenderNumberedList(JsonElement block)
    {
        var sb = new StringBuilder("  <ol>\n");
        if (block.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            foreach (var item in items.EnumerateArray())
            {
                var text = "";
                if (item.ValueKind == JsonValueKind.String) text = item.GetString() ?? "";
                else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("text", out var tp)) text = tp.GetString() ?? "";

                sb.AppendLine($"    <li>{Encode(text)}</li>");
            }
        sb.Append("  </ol>");
        return sb.ToString();
    }

    private static string RenderChecklist(JsonElement block)
    {
        var sb = new StringBuilder("  <ul class='checklist'>\n");
        if (block.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            foreach (var item in items.EnumerateArray())
            {
                var text = item.TryGetProperty("text", out var tp) ? tp.GetString() ?? "" : "";
                var isChecked = item.TryGetProperty("checked", out var cp) && cp.ValueKind == JsonValueKind.True;
                var checkedAttr = isChecked ? " checked disabled" : " disabled";
                sb.AppendLine($"    <li><input type='checkbox'{checkedAttr}> {Encode(text)}</li>");
            }
        sb.Append("  </ul>");
        return sb.ToString();
    }

    private static string RenderDivider(JsonElement block)
    {
        var style = block.TryGetProperty("style", out var sp) ? sp.GetString() ?? "singleLine" : "singleLine";
        var allowedStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "singleLine", "dots", "breakLines", "space", "doubleLine" };
        if (!allowedStyles.Contains(style)) style = "singleLine";
        return $"  <hr class='divider-{Encode(style)}'>";
    }

    private static string RenderLink(JsonElement block)
    {
        var url = block.TryGetProperty("url", out var up) ? up.GetString() ?? "" : "";
        var text = block.TryGetProperty("text", out var tp) ? tp.GetString() ?? url : url;
        if (!NoteContentValidator.IsSafeUrl(url))
            return $"  <p>[Unsafe link removed]</p>";
        return $"  <p><a href='{Encode(url)}' rel='noopener noreferrer' target='_blank'>{Encode(text)}</a></p>";
    }

    private static string RenderCode(JsonElement block)
    {
        var code = block.TryGetProperty("code", out var cp) ? cp.GetString() ?? "" : "";
        var lang = block.TryGetProperty("language", out var lp) ? lp.GetString() ?? "" : "";

        // Safe background color
        var bgColor = "#1f2937"; // default dark
        if (block.TryGetProperty("ui", out var uiEl) &&
            uiEl.TryGetProperty("backgroundColor", out var colorProp))
        {
            var candidate = colorProp.GetString() ?? "";
            if (System.Text.RegularExpressions.Regex.IsMatch(candidate, @"^#([0-9A-Fa-f]{3,4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$"))
                bgColor = candidate;
        }

        var codeId = Guid.NewGuid().ToString("N")[..8];
        return $@"  <div class='code-block' style='background:{bgColor};'>
    <button class='copy-btn' onclick='copyCode(""{codeId}"")'>Copy</button>
    <pre id='{codeId}' data-lang='{Encode(lang)}'>{Encode(code)}</pre>
  </div>
  <script>function copyCode(id){{var el=document.getElementById(id);navigator.clipboard.writeText(el.innerText);}}</script>";
    }

    private static string RenderDownloadAttachment(JsonElement block)
    {
        var displayName = block.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "Download" : "Download";
        // No raw storage key is exposed. The attachmentId would need an authenticated download endpoint.
        return $@"  <div class='file-card'>
    <div class='file-info'>
      <div class='file-name'>{Encode(displayName)}</div>
      <div class='file-size'>File attachment</div>
    </div>
    <span style='color:#6b7280;font-size:.85rem;'>Requires login to download</span>
  </div>";
    }

    private static string RenderCommandGenerator(JsonElement block)
    {
        var name = block.TryGetProperty("name", out var np) ? np.GetString() ?? "Command Generator" : "Command Generator";
        var desc = block.TryGetProperty("description", out var dp) ? dp.GetString() ?? "" : "";
        return $@"  <div style='border:1px solid #e5e7eb;border-radius:8px;padding:1rem;margin:.75rem 0;background:#f0f9ff;'>
    <strong>&#128736; {Encode(name)}</strong>
    {(string.IsNullOrEmpty(desc) ? "" : $"<p style='margin:.5rem 0 0;color:#374151;'>{Encode(desc)}</p>")}
    <p style='font-size:.8rem;color:#9ca3af;margin:.5rem 0 0;'>Open the app to use this generator.</p>
  </div>";
    }

    private static string RenderCopyCard(JsonElement block)
    {
        var text = block.TryGetProperty("text", out var tp) ? tp.GetString() ?? "" : "";
        var label = block.TryGetProperty("label", out var lp) ? lp.GetString() ?? "" : "";
        var codeId = Guid.NewGuid().ToString("N")[..8];
        
        return $@"  <div style='border:1px solid #e5e7eb;border-radius:8px;padding:1rem;margin:.75rem 0;background:#faf5ff;position:relative;'>
    {(string.IsNullOrEmpty(label) ? "" : $"<div style='font-size:.85rem;font-weight:600;color:#6b7280;margin-bottom:.5rem;'>{Encode(label)}</div>")}
    <div id='{codeId}' style='font-family:monospace;word-break:break-all;padding-right:4rem;'>{Encode(text)}</div>
    <button class='copy-btn' style='background:#9333ea;color:white;border:none;' onclick='copyCode(""{codeId}"")'>Copy</button>
  </div>";
    }

    private static string Encode(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
}
