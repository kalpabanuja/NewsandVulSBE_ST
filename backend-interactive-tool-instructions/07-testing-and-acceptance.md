# Part 07 — Testing and Acceptance

## Database

Test:

```text
ID generation
foreign keys
owner relationship
required fields
unique constraints
soft delete
asset versioning
content hash changes
```

## Ownership

Owner:

```text
create ✓
read ✓
update ✓
replace HTML ✓
replace CSS ✓
replace JavaScript ✓
delete ✓
```

Non-owner:

```text
view if note permission allows ✓
update ✗
replace HTML ✗
replace CSS ✗
replace JavaScript ✗
delete ✗
```

Unauthenticated management:

```text
denied
```

## Client-owner spoofing

Attempt to submit another user's ID.

Expected:

```text
ignored or rejected
actual authenticated/note owner remains authoritative
```

## HTML tests

Test:

```text
valid
malformed
oversized
dangerous tags
event handlers
unsafe URLs
unapproved external resources
```

## CSS tests

Test:

```text
valid
malformed
oversized
unsafe imports
unapproved external resources
```

## JavaScript tests

Test:

```text
valid syntax
invalid syntax
oversized
policy-rejected code
empty code where prohibited
```

CRUD must not execute the script.

## Revision tests

Example:

```text
asset version 1
HTML changed → 2
CSS changed → 3
JavaScript changed → 4
```

Old revisions remain immutable.

## Concurrency

Two clients edit the same tool.

Expected stale update:

```text
409 Conflict
```

No silent overwrite.

## Note integration

Test:

```text
note without tool
note with one tool
note with multiple tools
missing tool reference
deleted tool
duplicate note
note revisions
```

## Import

Imported HTML/CSS/JavaScript must go through the same validation rules.

Never execute imported code.

## Security acceptance

Reject the implementation if:

```text
non-owner can edit
non-owner can delete
client can choose owner
source leaks into logs
source leaks into exception messages
CRUD executes JavaScript
import executes code
tool CSS escapes its intended scope
old Command Generator remains the authoritative new model
```

## Definition of Done

```text
Legacy Command Generator assessed
Safe migration completed
Obsolete tables retired safely
New Custom Interactive Tool model exists

Unique ID works
Name works
Description works
HTML works
CSS works
JavaScript works
Validation/security state works
Versioning works
Hashing works
Owner relationship works

Owner-only editing enforced
Owner-only deletion enforced
Owner-only code replacement enforced

API works
EF migration works
Authorization tests pass
Validation tests pass
Concurrency tests pass
Import/export tests pass
Note integration tests pass
```
