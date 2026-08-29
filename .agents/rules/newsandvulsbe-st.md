---
trigger: always_on
---

# AGENTS.md

## Project Instructions

Before making any changes to this repository, read:

```text
docs/ai-instructions/00-master-index.md
```

The master index defines the project's architecture, implementation order, responsibilities, constraints, and references to the detailed instructions.

### Detailed Instructions

The detailed implementation instructions are located in:

```text
docs/ai-instructions/
```

Read the instruction file(s) relevant to the task before implementing anything.

Available instruction files:

```text
00-master-index.md
01-foundation-architecture.md
02-database-persistence.md
03-notes-search-crud.md
04-command-generator.md
05-sharing-import-export.md
06-api-security-integration.md
07-maui-client.md
08-testing-deployment-operations.md
09-section-map.md
```

### Important

* Follow the master index and relevant detailed instructions.
* Inspect the existing code before making changes.
* Do not unnecessarily rewrite or restructure existing code.
* Preserve existing architecture and contracts unless the instructions explicitly require a change.
* Treat security requirements as mandatory.
* Do not introduce unnecessary technologies or architecture.
* Implement tests for meaningful changes.
* Verify builds and tests before declaring work complete.
* If instructions conflict, stop and resolve the conflict using the master index and existing project structure rather than silently choosing an interpretation.

### Source of Truth

For implementation requirements, use:

```text
docs/ai-instructions/00-master-index.md
```

and the detailed instruction files referenced by it.

Do not attempt to duplicate the complete project specification inside this file.
