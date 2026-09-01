# Part 06 — Migration and Backward Compatibility

## Goal

Move from:

```text
Old Command Generator
```

to:

```text
Custom Interactive Tool
```

without losing existing notes.

## Before migration

Measure:

```text
legacy generator count
legacy field count
legacy option count
notes containing legacy generators
JSON references to legacy generator IDs
```

## Migration

Use an EF Core migration.

Recommended order:

```text
add new schema
→ migrate compatible data
→ verify
→ switch application to new model
→ remove obsolete schema when safe
```

Do not drop the old tables before verification.

## Compatibility

Do not claim that every old command template can be converted into HTML/CSS/JavaScript.

Only perform deterministic conversions.

For anything that cannot be safely converted:

```text
preserve/archive it
report it
do not silently discard it
```

## Legacy API

If production clients still use:

```text
/command-generators/*
```

do not abruptly remove them without a versioning/deprecation plan.

However, the new Custom Interactive Tool resource must become the authoritative implementation for the new feature.

## Rollback

Before deployment:

```text
database backup
migration verification
rollback plan
```

Never use a database reset as rollback.
