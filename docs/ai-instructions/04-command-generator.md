# AI Agent Implementation Instructions

This file is a focused implementation unit extracted from the larger backend/integration specification.

## How to use this file
- Treat the instructions in this file as authoritative for the sections it contains.
- Preserve the architecture and security constraints from the original specification.
- Do not silently remove requirements.
- When another part is referenced, use the master index to locate it rather than duplicating or redefining the requirement.
- Implement production-quality code and tests appropriate to the scope of this file.
- Do not introduce architecture that conflicts with the modular-monolith direction.

# Part 4 — C# Command Generator

## Source sections included

`25`, `26`, `27`, `28`, `29`, `30`, `31`, `32`, `33`, `34`, `35`, `36`, `37`, `38`, `39`, `40`, `41`, `70`, `78`, `97`, `98`, `99`

---

# 25. Command Generator

This is a major feature and should be implemented as a **safe template engine**, not as a shell executor.

The user's requirements describe a note containing a command form where the viewer selects values such as target/IP, operation and port selection, then presses a button to generate a command.

The backend should:

1. Store a command-generator definition.
2. Validate the definition.
3. Return its fields/options to MAUI.
4. Accept selected values.
5. Validate submitted values.
6. Generate the final command as a deterministic C# operation.
7. Return the generated command.
8. Never execute it automatically.

---


---

# 26. Critical Command Generator Security Rule

**The server must not execute generated commands.**

Do not implement:

```csharp
Process.Start(...)
```

inside the command-generation API.

Do not pass generated input directly to:

```text
bash
sh
cmd.exe
powershell
zsh
```

The initial feature is a **command generator**, not a remote shell.

This dramatically reduces the attack surface and keeps generation deterministic.

If command execution is ever added later, it must be a separate subsystem with a strict sandbox, explicit permissions, allowlisted binaries, OS isolation and resource controls.

---


---

# 27. Command Generator Data Model

Recommended table:

```sql
note_command_generators
id                 uuid primary key
note_id            uuid not null references notes(id) on delete cascade

name               varchar(150) not null
description        varchar(1000)

tool_name          varchar(100) not null
template           text not null

schema_jsonb       jsonb not null

is_enabled         boolean not null default true

created_at         timestamptz not null
updated_at         timestamptz not null
```

Example template:

```text
nmap {target} {scanType} {ports} {extraOptions}
```

---


---

# 28. Command Generator Field Definition

Example JSON:

```json
{
  "fields": [
    {
      "key": "target",
      "label": "Target",
      "type": "target",
      "required": true,
      "placeholder": "192.168.1.10"
    },
    {
      "key": "scanType",
      "label": "Scan Type",
      "type": "select",
      "required": true,
      "options": [
        {
          "value": "-sS",
          "label": "TCP SYN"
        },
        {
          "value": "-sT",
          "label": "TCP Connect"
        }
      ]
    },
    {
      "key": "ports",
      "label": "Ports",
      "type": "portSelector",
      "required": false
    }
  ]
}
```

---


---

# 29. Supported Command Field Types

Start with:

```text
text
target
select
multiSelect
boolean
integer
portSelector
preset
```

Potential later additions:

```text
hostname
url
filePath
enum
range
customExpression
```

Every field must have strict validation.

---


---

# 30. Target Validation

A `target` field should not accept arbitrary shell syntax.

Support:

```text
IPv4
IPv6
hostname
CIDR
```

Depending on the tool.

Example valid values:

```text
192.168.1.10
192.168.1.0/24
2001:db8::1
scan.example.com
```

Do not allow control characters.

Reject:

```text
;
&&
||
|
>
<
`
$()
```

and embedded newline characters.

---


---

# 31. Port Selection

The requirements specifically mention selecting:

- all ports
- specific ports
- commonly used ports
- individual port ranges

Implement a dedicated port selector.

Accepted logical representations:

```json
{
  "mode": "all"
}
```

```json
{
  "mode": "common"
}
```

```json
{
  "mode": "list",
  "ports": [22, 80, 443]
}
```

```json
{
  "mode": "range",
  "from": 1,
  "to": 1024
}
```

Server validation must enforce:

```text
1 <= port <= 65535
```

and reject invalid ranges.

---


---

# 32. Port Presets

Do not permanently hard-code one universal "common ports" list in multiple layers.

Create a versioned preset model.

Example:

```json
{
  "id": "common",
  "name": "Common Ports",
  "version": 1,
  "ports": [21,22,23,25,53,80,110,139,143,443,445,3389]
}
```

This allows the preset to change without changing the command-generator engine.

---


---

# 33. Command Template Syntax

Use a deliberately small placeholder syntax:

```text
{target}
{ports}
{scanType}
```

Do not use a general-purpose scripting language.

Never allow templates to evaluate arbitrary C#.

Never allow templates to contain:

```text
C# expressions
reflection
method calls
shell expressions
SQL
JavaScript
```

---


---

# 34. C# Command Generator Contract

Recommended interface:

```csharp
public interface ICommandGenerator
{
    CommandGenerationResult Generate(
        CommandGeneratorDefinition definition,
        IReadOnlyDictionary<string, object?> values);
}
```

Result:

```csharp
public sealed record CommandGenerationResult(
    bool Success,
    string? Command,
    IReadOnlyList<ValidationError> Errors);
```

---


---

# 35. Suggested C# Models

```csharp
public sealed record CommandGeneratorDefinition(
    Guid Id,
    string Name,
    string ToolName,
    string Template,
    IReadOnlyList<CommandFieldDefinition> Fields);

public sealed record CommandFieldDefinition(
    string Key,
    string Label,
    CommandFieldType Type,
    bool Required,
    IReadOnlyList<CommandOption>? Options);

public enum CommandFieldType
{
    Text,
    Target,
    Select,
    MultiSelect,
    Boolean,
    Integer,
    PortSelector,
    Preset
}
```

---


---

# 36. Generator Algorithm

The generator should follow this exact sequence:

```text
1. Load generator definition.
2. Verify generator belongs to an accessible note.
3. Verify generator is enabled.
4. Validate requested field keys.
5. Reject unknown fields.
6. Validate required fields.
7. Validate each field according to its declared type.
8. Normalize values.
9. Resolve presets.
10. Render placeholders.
11. Verify no unresolved placeholders remain.
12. Verify the generated command contains no forbidden control characters.
13. Return command.
```

---


---

# 37. C# Template Rendering

Use a custom renderer rather than unrestricted string interpolation.

Example:

```csharp
public sealed class CommandTemplateRenderer
{
    public string Render(
        string template,
        IReadOnlyDictionary<string, string> values)
    {
        var result = template;

        foreach (var pair in values)
        {
            result = result.Replace(
                "{" + pair.Key + "}",
                pair.Value,
                StringComparison.Ordinal);
        }

        if (Regex.IsMatch(result, @"\{[^{}]+\}"))
        {
            throw new ValidationException(
                "Template contains unresolved placeholders.");
        }

        return result;
    }
}
```

For production, also enforce:

```text
maximum template length
maximum generated command length
allowed placeholder names
no control characters
```

---


---

# 38. Command Generation API

Endpoint:

```http
POST /api/v1/command-generators/{id}/generate
```

Request:

```json
{
  "values": {
    "target": "192.168.1.10",
    "scanType": "-sS",
    "ports": {
      "mode": "common"
    }
  }
}
```

Response:

```json
{
  "command": "nmap -sS -p 21,22,23,25,53,80,110,139,143,443,445,3389 192.168.1.10"
}
```

The returned command is data only. It is not executed by the server.

---


---

# 39. Command Generator Validation Errors

Return structured errors:

```json
{
  "errors": [
    {
      "field": "target",
      "code": "invalid_target",
      "message": "Enter a valid IP address, hostname or CIDR."
    }
  ]
}
```

MAUI can use the field key to display the message beside the relevant control.

---


---

# 40. Tool Definitions

Because the application will support multiple technical tools, consider introducing:

```text
tools
tool_versions
tool_command_templates
```

Example conceptual model:

```text
Tool: Nmap
Version: 7.x
Generator:
  Full TCP scan
  SYN scan
  UDP scan
  Service detection
  OS detection
```

Do not make the backend dependent on one hard-coded tool.

---


---

# 41. Command Templates Should Be Versioned

Command syntax changes over time.

Store:

```text
toolName
toolVersion
templateVersion
```

This makes existing notes reproducible.

Example:

```text
Nmap
7.x
template v2
```

A revised template should create a new revision rather than silently mutating historical behavior.

---


---

# 70. Command Generator Abuse Prevention

A command-generator definition is data and must not become code.

Reject:

```text
dynamic C# expressions
Roslyn execution
PowerShell expressions
shell fragments
SQL fragments
embedded scripts
```

The generator should be equivalent to:

```text
validated inputs
        +
safe placeholders
        ->
deterministic string
```

---


---

# 78. Command Generator Test Examples

Given:

```text
template:
nmap {scanType} {ports} {target}
```

and:

```text
scanType = -sS
ports = -p 80,443
target = 192.168.1.10
```

expect:

```text
nmap -sS -p 80,443 192.168.1.10
```

Test rejection of:

```text
target = 192.168.1.10 && whoami
```

and:

```text
target = 192.168.1.10; cat /etc/passwd
```

The server should reject the value before template rendering.

---


---

# 97. Generated Command UI Contract

The API should return enough metadata for MAUI to dynamically create the form.

For example:

```json
{
  "id": "...",
  "name": "Nmap Scan",
  "toolName": "nmap",
  "fields": [
    {
      "key": "target",
      "label": "Target",
      "type": "target",
      "required": true
    },
    {
      "key": "ports",
      "label": "Ports",
      "type": "portSelector",
      "required": false,
      "presets": [
        "common",
        "all"
      ]
    }
  ]
}
```

MAUI can then render the form without hard-coding every individual command-generator page.

---


---

# 98. Generated Command UX

After generation, the API should return:

```json
{
  "command": "nmap ...",
  "displayCommand": "nmap ...",
  "warnings": []
}
```

The MAUI UI should support:

```text
Copy
Edit inputs
Regenerate
Save as preset
Share/copy
```

The first implementation should not have an "Execute" button.

---


---

# 99. Optional Saved Command Presets

A useful future feature is:

```text
SavedCommandPreset
```

Example:

```text
Name:
My Common Nmap Scan

Generator:
Nmap Scan

Values:
target = ...
ports = common
scanType = SYN
```

This allows users to regenerate commands quickly without rebuilding the form.

---
