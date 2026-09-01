# Part 04 — Validation and Secure Code Storage

## Core rule

HTML, CSS, and JavaScript are untrusted assets.

Validate each independently.

## HTML

Validate:

```text
encoding
size
markup validity
dangerous tags
event-handler attributes
unsafe URL schemes
external resources
```

The exact allowlist must match the final rendering architecture.

## CSS

Validate:

```text
encoding
size
syntax
imports
external resources
```

CSS must be scoped to the interactive-tool container rather than becoming unrestricted application-wide CSS.

## JavaScript

Validate:

```text
encoding
size
syntax/parsing
application security policy
```

Do not execute it during:

```text
create
read
update
delete
import
```

unless a separate, explicitly secured execution feature is intentionally designed.

## Important security principle

A regex blacklist is not a sandbox.

Do not rely on checks such as:

```text
"eval"
"document"
"window"
"fetch"
```

as the primary security mechanism.

If JavaScript is rendered/executed in a browser, isolate it from the authenticated MAUI/application DOM and credentials using the chosen frontend architecture, sandboxing, and CSP.

If server-side execution is introduced later, it must have a real execution boundary with:

```text
no filesystem
no process creation
no secrets
network disabled by default
strict timeout
resource limits
stateless execution
```

## Size limits

Recommended configurable starting limits:

```text
HTML: 256 KB
CSS: 256 KB
JavaScript: 256 KB
Total tool assets: 768 KB
```

Return an appropriate payload-too-large response when exceeded.

## Secure storage/logging

Never place source code in:

```text
ordinary logs
analytics
telemetry
exception messages
```

Preserve source exactly where meaningful; do not silently rewrite user code.

## Hash

Recalculate the content hash whenever any source or relevant canonical metadata changes.

## Revision/audit

Where existing revision/audit infrastructure supports it, record:

```text
InteractiveToolCreated
InteractiveToolUpdated
InteractiveToolDeleted
InteractiveToolRestored
```

Do not put the complete source into ordinary audit logs.

Historical source revisions must be protected by the same ownership rules.

## No hidden execution

CRUD must never:

```text
run JavaScript
launch a shell
execute an OS command
call arbitrary network endpoints
```
