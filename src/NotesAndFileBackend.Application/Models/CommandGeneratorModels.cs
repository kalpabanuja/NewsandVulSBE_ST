using NotesAndFileBackend.Domain.Enums;
using System.Text.Json;

namespace NotesAndFileBackend.Application.Models;

public class CommandGeneratorDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public List<CommandFieldDefinition> Fields { get; set; } = new List<CommandFieldDefinition>();
}

public class CommandFieldDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CommandFieldType Type { get; set; }
    public bool Required { get; set; }
    public List<CommandOption>? Options { get; set; }
    public string? Placeholder { get; set; }
    public List<string>? Presets { get; set; }
}

public class CommandOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class CommandGenerationResultDto
{
    public bool Success { get; set; }
    public string? Command { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
}

public class ValidationErrorDto
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
