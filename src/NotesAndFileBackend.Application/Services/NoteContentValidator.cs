using System.Text.Json;

namespace NotesAndFileBackend.Application.Services;

/// <summary>
/// Validates the canonical structured note content document.
/// All block types and their properties are validated against allowlists.
/// No HTML injection or dangerous URLs are permitted.
/// </summary>
public static class NoteContentValidator
{
    private static readonly HashSet<string> AllowedBlockTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "heading", "paragraph", "bulletList", "numberedList", "checkList",
        "divider", "link", "displayAttachment", "downloadAttachment",
        "code", "commandGenerator", "copyCard"
    };

    private static readonly HashSet<string> AllowedBulletStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "disc", "circle", "square", "dash"
    };

    private static readonly HashSet<string> AllowedDividerStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "singleLine", "dots", "breakLines", "space", "doubleLine"
    };

    private static readonly HashSet<string> AllowedUrlSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https"
    };

    private static readonly HashSet<string> DangerousUrlSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "javascript", "data", "file", "vbscript"
    };

    // Hex color: #RGB, #RGBA, #RRGGBB, #RRGGBBAA
    private static readonly System.Text.RegularExpressions.Regex HexColorRegex =
        new(@"^#([0-9A-Fa-f]{3,4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$");

    public record ValidationError(string Field, string Code, string Message);

    public static List<ValidationError> Validate(string contentJson)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(contentJson))
        {
            errors.Add(new ValidationError("content", "required", "Note content is required."));
            return errors;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(contentJson);
        }
        catch (JsonException)
        {
            errors.Add(new ValidationError("content", "invalid_json", "Note content is not valid JSON."));
            return errors;
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new ValidationError("content", "invalid_type", "Content must be a JSON object."));
                return errors;
            }

            if (!root.TryGetProperty("blocks", out var blocksEl) || blocksEl.ValueKind != JsonValueKind.Array)
            {
                errors.Add(new ValidationError("content.blocks", "required", "Content must have a 'blocks' array."));
                return errors;
            }

            var seenIds = new HashSet<string>();
            var idx = 0;

            foreach (var block in blocksEl.EnumerateArray())
            {
                var prefix = $"content.blocks[{idx}]";

                // Validate block ID uniqueness
                var blockId = block.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String ? idProp.GetString() : null;
                if (string.IsNullOrWhiteSpace(blockId))
                {
                    errors.Add(new ValidationError($"{prefix}.id", "required", "Each block must have a non-empty string 'id'."));
                }
                else if (!seenIds.Add(blockId))
                {
                    errors.Add(new ValidationError($"{prefix}.id", "duplicate_id", $"Duplicate block id '{blockId}'."));
                }

                // Validate block type
                var blockType = block.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String ? typeProp.GetString() : null;
                if (string.IsNullOrWhiteSpace(blockType))
                {
                    errors.Add(new ValidationError($"{prefix}.type", "required", "Each block must have a string 'type'."));
                    idx++;
                    continue;
                }

                if (!AllowedBlockTypes.Contains(blockType))
                {
                    errors.Add(new ValidationError($"{prefix}.type", "unsupported_block_type",
                        $"Block type '{blockType}' is not supported. Allowed: {string.Join(", ", AllowedBlockTypes)}."));
                    idx++;
                    continue;
                }

                // Type-specific validation
                switch (blockType.ToLowerInvariant())
                {
                    case "heading":
                        ValidateHeadingBlock(block, prefix, errors);
                        break;
                    case "bulletlist":
                        ValidateBulletListBlock(block, prefix, errors);
                        break;
                    case "divider":
                        ValidateDividerBlock(block, prefix, errors);
                        break;
                    case "link":
                        ValidateLinkBlock(block, prefix, errors);
                        break;
                    case "code":
                        ValidateCodeBlock(block, prefix, errors);
                        break;
                    case "displayattachment":
                    case "downloadattachment":
                        ValidateAttachmentBlock(block, prefix, errors, blockType);
                        break;
                    case "copycard":
                        ValidateCopyCardBlock(block, prefix, errors);
                        break;
                }

                idx++;
            }
        }

        return errors;
    }

    private static void ValidateHeadingBlock(JsonElement block, string prefix, List<ValidationError> errors)
    {
        if (!block.TryGetProperty("level", out var levelProp) || levelProp.ValueKind != JsonValueKind.Number)
        {
            errors.Add(new ValidationError($"{prefix}.level", "required", "Heading block must have an integer 'level'."));
            return;
        }

        var level = levelProp.GetInt32();
        if (level < 1 || level > 5)
        {
            errors.Add(new ValidationError($"{prefix}.level", "invalid_heading_level",
                "Heading level must be between 1 and 5."));
        }

        if (block.TryGetProperty("text", out var textProp))
        {
            if (textProp.ValueKind != JsonValueKind.String)
            {
                errors.Add(new ValidationError($"{prefix}.text", "invalid_type", "Heading text must be a string."));
            }
            else if (textProp.GetString()?.Length > 300)
            {
                errors.Add(new ValidationError($"{prefix}.text", "too_long", "Heading text cannot exceed 300 characters."));
            }
        }
    }

    private static void ValidateBulletListBlock(JsonElement block, string prefix, List<ValidationError> errors)
    {
        if (block.TryGetProperty("style", out var styleProp) && styleProp.ValueKind == JsonValueKind.String)
        {
            var style = styleProp.GetString();
            if (!string.IsNullOrEmpty(style) && !AllowedBulletStyles.Contains(style))
            {
                errors.Add(new ValidationError($"{prefix}.style", "invalid_bullet_style",
                    $"Bullet style '{style}' is not supported. Allowed: {string.Join(", ", AllowedBulletStyles)}."));
            }
        }
    }

    private static void ValidateDividerBlock(JsonElement block, string prefix, List<ValidationError> errors)
    {
        if (block.TryGetProperty("style", out var styleProp) && styleProp.ValueKind == JsonValueKind.String)
        {
            var style = styleProp.GetString();
            if (!string.IsNullOrEmpty(style) && !AllowedDividerStyles.Contains(style))
            {
                errors.Add(new ValidationError($"{prefix}.style", "invalid_divider_style",
                    $"Divider style '{style}' is not supported. Allowed: {string.Join(", ", AllowedDividerStyles)}."));
            }
        }
    }

    private static void ValidateLinkBlock(JsonElement block, string prefix, List<ValidationError> errors)
    {
        if (!block.TryGetProperty("url", out var urlProp) || urlProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(urlProp.GetString()))
        {
            errors.Add(new ValidationError($"{prefix}.url", "required", "Link block must have a non-empty string 'url'."));
            return;
        }

        var urlStr = urlProp.GetString()!;
        if (!Uri.TryCreate(urlStr, UriKind.Absolute, out var uri))
        {
            errors.Add(new ValidationError($"{prefix}.url", "invalid_url", "Link URL is not a valid absolute URL."));
            return;
        }

        if (DangerousUrlSchemes.Contains(uri.Scheme))
        {
            errors.Add(new ValidationError($"{prefix}.url", "dangerous_url_scheme",
                $"URL scheme '{uri.Scheme}' is not allowed for security reasons."));
            return;
        }

        if (!AllowedUrlSchemes.Contains(uri.Scheme))
        {
            errors.Add(new ValidationError($"{prefix}.url", "unsupported_url_scheme",
                $"URL scheme '{uri.Scheme}' is not supported. Use http or https."));
        }
    }

    private static void ValidateCodeBlock(JsonElement block, string prefix, List<ValidationError> errors)
    {
        if (!block.TryGetProperty("code", out var codeProp) || codeProp.ValueKind != JsonValueKind.String)
        {
            errors.Add(new ValidationError($"{prefix}.code", "required", "Code block must have a 'code' string."));
        }

        // Validate optional UI backgroundColor
        if (block.TryGetProperty("ui", out var uiProp) && uiProp.ValueKind == JsonValueKind.Object)
        {
            if (uiProp.TryGetProperty("backgroundColor", out var colorProp) && colorProp.ValueKind == JsonValueKind.String)
            {
                var color = colorProp.GetString();
                if (!string.IsNullOrEmpty(color) && !HexColorRegex.IsMatch(color))
                {
                    errors.Add(new ValidationError($"{prefix}.ui.backgroundColor", "invalid_color",
                        "backgroundColor must be a valid hex color (#RGB, #RGBA, #RRGGBB, or #RRGGBBAA)."));
                }
            }
        }
    }

    private static void ValidateAttachmentBlock(JsonElement block, string prefix, List<ValidationError> errors, string blockType)
    {
        // Attachment blocks must reference an attachmentId — not a raw storage URL
        if (!block.TryGetProperty("attachmentId", out var attIdProp) ||
            attIdProp.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(attIdProp.GetString()))
        {
            errors.Add(new ValidationError($"{prefix}.attachmentId", "required",
                $"{blockType} block must reference a string 'attachmentId'."));
        }
    }

    private static void ValidateCopyCardBlock(JsonElement block, string prefix, List<ValidationError> errors)
    {
        // CopyCard must have a non-empty 'text' field — the content the user copies to clipboard.
        if (!block.TryGetProperty("text", out var textProp) ||
            textProp.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(textProp.GetString()))
        {
            errors.Add(new ValidationError($"{prefix}.text", "required",
                "CopyCard block must have a non-empty string 'text' field."));
        }
        else if (textProp.GetString()!.Length > 5000)
        {
            errors.Add(new ValidationError($"{prefix}.text", "too_long",
                "CopyCard text cannot exceed 5000 characters."));
        }

        // Optional label/title for the card
        if (block.TryGetProperty("label", out var labelProp) && labelProp.ValueKind != JsonValueKind.String)
        {
            errors.Add(new ValidationError($"{prefix}.label", "invalid_type",
                "CopyCard label must be a string if provided."));
        }
    }

    /// <summary>
    /// Returns true if the URL is safe to render in HTML (http/https only, parseable).
    /// Use when generating HTML share pages.
    /// </summary>
    public static bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (DangerousUrlSchemes.Contains(uri.Scheme)) return false;
        return AllowedUrlSchemes.Contains(uri.Scheme);
    }
}
