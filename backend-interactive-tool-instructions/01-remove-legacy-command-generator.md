# Part 01 — Remove the Legacy Command Generator

## Goal

Remove the previous Command Generator implementation and replace it with Custom Interactive Tools.

First search for all legacy references, including:

```text
note_command_generators
command_generator_fields
command_generator_options
CommandGenerator
CommandGeneratorDefinition
CommandFieldDefinition
CommandFieldType
ICommandGenerator
CommandTemplateRenderer
CommandGenerationResult
/command-generators
csharp_template
portSelector
preset
```

Do not delete names blindly; confirm each is exclusively part of the legacy subsystem.

## Remove/migrate

Retire or migrate:

- legacy database tables
- EF entities/configurations
- repositories
- services
- validators
- DTOs
- command-template renderer
- field/preset models
- obsolete endpoints
- obsolete tests

## Database

Use an EF Core migration.

Never:

- manually drop production tables;
- reset the database;
- silently delete existing note content.

Before removing legacy tables:

```text
identify old generator records
→ identify notes containing them
→ preserve/migrate compatible data
→ verify
→ remove obsolete schema
```

## Important

C# remains the backend implementation language.

The old user-authored command-template system is being replaced by user-authored:

```text
HTML
CSS
JavaScript
```

Do not leave the old Command Generator as the authoritative model for the new block.
