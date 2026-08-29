using System.ComponentModel.DataAnnotations;

namespace NotesAndFileBackend.Api.DTOs;

public class CreateCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int SortOrder { get; set; } = 0;
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}
