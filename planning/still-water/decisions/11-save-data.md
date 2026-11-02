# Save Data

- **Status:** Open (draft proposed)
- **Type:** domain-model
- **Blocks:** Phase 2

## Question
How is progress stored on the phone, and what goes in it?

## Why it matters
Lab R5: progress must survive a force-close. Paper guide question 1 asks where it's stored.

## Options
| Option | For | Against |
|---|---|---|
| PlayerPrefs | One-liners | Awkward for dictionaries; less clear to explain |
| **JSON file in `Application.persistentDataPath`** | Clear schema, easy to version and debug; good for the paper | Slightly more code |
| SQLite | Overkill for this | Plugin hassle |

## Proposed schema
See [SYSTEM_ATLAS.md §5](../../../docs/SYSTEM_ATLAS.md#5-save-data-draft--save-data). Atomic write (temp file → replace). `version` field for future migrations.

## Edge cases to validate
- A corrupt or missing file → start fresh with defaults (never crash).
- A clear of level 5 does not try to unlock level 6.
- Replaying an old level keeps the best stars/time (max, not overwrite).
- Settings save immediately when changed.

## Recommendation
JSON file. Lock it as proposed.

## Decision
_(fill in when locked)_
