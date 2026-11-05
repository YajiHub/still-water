# Save Data

- **Status:** Locked (2026-11-05) (by: me)
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
**JSON file `save.json` in `Application.persistentDataPath`, per-level records keyed by name.**

```json
{
  "version": 1,
  "levels": [
    { "id": "level01", "cleared": true,  "bestStars": 2, "bestTime": 48.2 },
    { "id": "level02", "cleared": false, "bestStars": 0, "bestTime": 0 }
  ],
  "settings": { "sfxVol": 1.0, "musicVol": 0.8, "vibration": true, "lookSensitivity": 1.0, "brightness": 1.0 },
  "seenTutorial": false
}
```

- **Research:** Unity `JsonUtility` (used for level layouts) **can't serialize dictionaries**, and Newtonsoft JSON isn't installed (`Packages/packages-lock.json`), so the old draft's `"bestStars": {"1": 3}` would silently not save. Lists of records work.
- **Unlock rule:** a level is open if it's the first level, or the level before it is `cleared`. No separate `unlockedLevel` counter to drift out of sync. Clearing the last level unlocks nothing.
- **Ids are names** (`"level01"`), so worlds/chapters can be added later (`"w2-level01"`) without a schema change.
- **Best values only go up:** replaying keeps max stars and min time.
- **Write safely:** write `save.json.tmp`, then replace `save.json`, so a crash mid-save can't corrupt it. Save happens **before** the Result screen (lab R5). Settings save when changed.
- **Corrupt or missing file → defaults** (log a warning, never crash). Unknown future `version` → defaults too (no migration code until a v2 exists).
- Hidden debug **Reset progress** button (Settings) for testing and the demo.
- Progression UX: Level Select with locks and stars, plus **Next** on the Result screen for continuous play (lab R3/R4).

## Consequences
- Pure C# `ProgressRules` (unlock check, merge best stars/time) gets EditMode tests; `SaveSystem` only does file I/O.
- Paper guide Q1 answer: "unlocks are derived from `cleared` flags in a JSON file in the app's private storage, written atomically before the Result screen."
- The user's "continuous mode" idea (walk through the door into the next hall, go back, two players together) is recorded in [IDEAS.md](../../../docs/IDEAS.md); its co-op half is an option for [Online Match Format](./07-online-match-format.md).
