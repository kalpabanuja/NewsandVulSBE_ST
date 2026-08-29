using System.Text.Json;
using NotesAndFileBackend.Application.Models;
using NotesAndFileBackend.Domain.Enums;

namespace NotesAndFileBackend.Application.Services;

public interface ICommandGenerator
{
    CommandGenerationResultDto Generate(CommandGeneratorDefinition definition, Dictionary<string, JsonElement> values);
}

public class CommandGeneratorService : ICommandGenerator
{
    private readonly CommandTemplateRenderer _renderer;

    public CommandGeneratorService()
    {
        _renderer = new CommandTemplateRenderer();
    }

    public CommandGenerationResultDto Generate(CommandGeneratorDefinition definition, Dictionary<string, JsonElement> values)
    {
        var errors = new List<ValidationErrorDto>();
        var renderedValues = new Dictionary<string, string>();

        foreach (var field in definition.Fields)
        {
            if (!values.TryGetValue(field.Key, out var valueElement))
            {
                if (field.Required)
                {
                    errors.Add(new ValidationErrorDto { Field = field.Key, Code = "required", Message = "This field is required." });
                }
                else
                {
                    renderedValues[field.Key] = string.Empty; // provide empty string for missing non-required fields
                }
                continue;
            }

            var stringValue = valueElement.ValueKind == JsonValueKind.String ? valueElement.GetString() : valueElement.GetRawText();

            switch (field.Type)
            {
                case CommandFieldType.Target:
                    if (!CommandFieldValidators.IsValidTarget(stringValue ?? string.Empty))
                    {
                        errors.Add(new ValidationErrorDto { Field = field.Key, Code = "invalid_target", Message = "Enter a valid IP address, hostname or CIDR." });
                    }
                    else
                    {
                        renderedValues[field.Key] = stringValue!;
                    }
                    break;
                case CommandFieldType.PortSelector:
                    var (isValid, portArg, err) = CommandFieldValidators.ValidatePortSelector(valueElement);
                    if (!isValid)
                    {
                        errors.Add(new ValidationErrorDto { Field = field.Key, Code = "invalid_port", Message = err ?? "Invalid port selection." });
                    }
                    else
                    {
                        renderedValues[field.Key] = portArg!;
                    }
                    break;
                case CommandFieldType.Select:
                    if (field.Options == null || !field.Options.Any(o => o.Value == stringValue))
                    {
                        errors.Add(new ValidationErrorDto { Field = field.Key, Code = "invalid_selection", Message = "Invalid option selected." });
                    }
                    else
                    {
                        renderedValues[field.Key] = stringValue!;
                    }
                    break;
                default:
                    // Basic text or other types without strict specific rules
                    if (CommandFieldValidators.ContainsControlCharacters(stringValue ?? string.Empty))
                    {
                        errors.Add(new ValidationErrorDto { Field = field.Key, Code = "invalid_chars", Message = "Forbidden characters detected." });
                    }
                    else
                    {
                        renderedValues[field.Key] = stringValue!;
                    }
                    break;
            }
        }

        if (errors.Any())
        {
            return new CommandGenerationResultDto { Success = false, Errors = errors };
        }

        try
        {
            var command = _renderer.Render(definition.Template, renderedValues);
            // Replace multiple spaces from missing optional params with a single space
            command = System.Text.RegularExpressions.Regex.Replace(command, @"\s+", " ").Trim();
            
            return new CommandGenerationResultDto { Success = true, Command = command };
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationErrorDto { Field = "template", Code = "render_error", Message = ex.Message });
            return new CommandGenerationResultDto { Success = false, Errors = errors };
        }
    }
}
