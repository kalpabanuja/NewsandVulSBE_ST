---
trigger: always_on
---

# AGENTS.md

## Purpose

This is an **existing backend**.

Your job is to **modify, migrate, and improve the existing backend**. Do not rebuild it from scratch.

## MUST

- Inspect the existing backend before changing anything.
- Reuse existing .NET, PostgreSQL, EF Core, services, repositories, and API patterns.
- Follow the `backend-interactive-tool-instructions/` files.
- Use EF Core migrations for database changes.
- Preserve existing note/data unless a migration explicitly handles it.
- Enforce ownership on the backend.
- Build and test after changes.

## DO NOT

- Do not recreate the backend.
- Do not reset/drop the production database.
- Do not create a second implementation of the same feature.
- Do not trust a client-supplied owner/user ID.
- Do not expose database entities directly through the API.
- Do not log HTML/CSS/JavaScript source.
- Do not execute uploaded code during CRUD/import.

## New Interactive Tool

The old Command Generator system is being replaced with:

```text
Custom Interactive Tool
 ├── ID
 ├── Name
 ├── Description
 ├── HTML
 ├── CSS
 ├── JavaScript
 └── Owner
```

Only the **note creator/tool owner** can:

```text
edit
change code
change metadata
delete
```

Other users may only view/use it when permitted.

## Instructions

Start with:

```text
backend-interactive-tool-instructions/00-backend-interactive-tools-master-index.md
```

Then follow:

```text
01 → 02 → 03 → 04 → 05 → 06 → 07
```

## Workflow

```text
Inspect
→ Reuse existing code
→ Migrate/refactor
→ Implement
→ Build
→ Test
```

Do not replace the existing backend.