# Backend Instruction Pack — Agent Work Breakdown

## Purpose

The original implementation instruction file is large and combines multiple responsibilities in one document. This pack breaks it into smaller agent-sized work units while keeping the original requirements intact.

The original source has 118 numbered sections and covers the .NET backend, PostgreSQL, MAUI integration, C# command generator, security, sharing, import/export, testing, deployment, and operations. fileciteturn1file0L11-L26

## Recommended execution order

1. `01-foundation-architecture.md`
2. `02-database-persistence.md`
3. `03-notes-search-crud.md`
4. `04-command-generator.md`
5. `05-sharing-import-export.md`
6. `06-api-security-integration.md`
7. `07-maui-client.md`
8. `08-testing-deployment-operations.md`

## Agent roles

### Agent 1 — Foundation
Owns solution structure, layers, domain boundaries, and the structured note document model.

### Agent 2 — Database
Owns PostgreSQL schema, EF Core mappings/migrations, constraints, indexes, transactions, concurrency, and persistence behavior.

### Agent 3 — Notes & Search
Owns note CRUD, content/search behavior, pagination/list optimization, archive/delete/duplicate behavior, and search UX contracts.

### Agent 4 — Command Generator
Owns the C# deterministic template engine, field types, target/port validation, presets, generator API, and generator security.

### Agent 5 — Sharing & Import/Export
Owns share links, public access rules, slug security, import/export formats, validation, and background processing for larger jobs.

### Agent 6 — API & Security
Owns authentication, authorization, error contracts, API versioning, rate limiting, validation, configuration, secure headers/CORS, and shared backend policies.

### Agent 7 — MAUI
Owns the client/API contract, DTO expectations, local cache/offline strategy, command-generator UI contract, and client-side validation boundaries.

### Agent 8 — Testing/Operations
Owns automated tests, Docker/deployment, observability, health checks, backups, performance targets, and release hardening.

## Important shared rules

The following must remain true across every agent:
- MAUI is an untrusted client; the API is the security boundary.
- PostgreSQL is private infrastructure; MAUI must never connect directly to it.
- User ownership is determined from the authenticated identity, never from a client-supplied `userId`.
- Generated commands are text only; the backend must never execute them.
- Imported files/content are untrusted input and must never trigger execution.
- Public share links must be treated as access capabilities and must support revocation.
- Production secrets must never be committed to Git or placed in the MAUI application.
- Use the modular-monolith approach; do not introduce microservices/event sourcing/Kafka/Kubernetes for the first version.

## Anti-conflict rule

When implementing across multiple agents:
- Do not invent a second database schema for the same entity.
- Do not create duplicate DTO names with different meanings.
- Do not move security-critical validation into MAUI-only code.
- Do not replace the deterministic C# generator with a scripting engine.
- Keep `/api/v1/` as the API versioning boundary.
- Keep the backend authoritative over persistence, authorization, validation, search, sharing, import/export, and command-generation rules.

## Definition of done

The original specification's acceptance criteria cover Notes, Search, Import/Export, Sharing, Command Generator, and Infrastructure. fileciteturn4file0L379-L456

Each agent should finish with:
1. Implementation
2. Unit/integration/API tests appropriate to its scope
3. Updated OpenAPI/contracts where applicable
4. Any EF Core migration required by its changes
5. A concise implementation summary and known follow-up items
