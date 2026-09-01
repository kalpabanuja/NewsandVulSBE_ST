# Backend Custom Interactive Tools — Master Index

## Purpose

Replace the previous Command Generator backend subsystem with a new **Custom Interactive Tool** subsystem.

This is a migration/rework task, not an additive feature.

### Mandatory rules

- Remove/migrate obsolete Command Generator database tables, entities, services, validators, DTOs, endpoints, and tests where they belong exclusively to the old subsystem.
- Do not create two competing systems for the same note block.
- Preserve existing notes and data through a controlled EF Core migration.
- The new model must support a unique ID, name, description, HTML, CSS, JavaScript, validation/security state, versioning, hashing, and ownership.
- Only the note creator/tool owner may modify or delete the tool/code.
- Never trust a client-supplied owner ID.
- Treat HTML/CSS/JavaScript as untrusted content.
- Do not execute uploaded JavaScript during ordinary CRUD.
- PostgreSQL remains the source of truth.

## New concept

```text
Note
 └── Custom Interactive Tool
       ├── id
       ├── name
       ├── description
       ├── html
       ├── css
       ├── javascript
       ├── validation/security state
       ├── version/hash
       └── owner
```

## Parts

1. `01-remove-legacy-command-generator.md`
2. `02-new-data-model-and-database.md`
3. `03-api-and-authorization.md`
4. `04-validation-and-secure-code-storage.md`
5. `05-service-layer-and-note-integration.md`
6. `06-migration-and-backward-compatibility.md`
7. `07-testing-and-acceptance.md`

Read this file first, then the relevant parts.
