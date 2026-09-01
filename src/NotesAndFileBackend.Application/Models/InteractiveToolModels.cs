using System;
using System.ComponentModel.DataAnnotations;

namespace NotesAndFileBackend.Application.Models;

public class InteractiveToolListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AssetVersion { get; set; }
    public bool IsEnabled { get; set; }
}

public class InteractiveToolDetailsDto : InteractiveToolListDto
{
    public string HtmlSource { get; set; } = string.Empty;
    public string CssSource { get; set; } = string.Empty;
    public string JavascriptSource { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public string SecurityStatus { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
}

public class CreateInteractiveToolRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(2048)]
    public string? Description { get; set; }
    
    [Required]
    public string HtmlSource { get; set; } = string.Empty;
    
    [Required]
    public string CssSource { get; set; } = string.Empty;
    
    [Required]
    public string JavascriptSource { get; set; } = string.Empty;
}

public class UpdateInteractiveToolRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(2048)]
    public string? Description { get; set; }
    
    [Required]
    public string HtmlSource { get; set; } = string.Empty;
    
    [Required]
    public string CssSource { get; set; } = string.Empty;
    
    [Required]
    public string JavascriptSource { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
}
