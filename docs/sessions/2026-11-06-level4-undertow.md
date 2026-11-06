# 2026-11-06 — Level 4 "Undertow" (sinking tiles)

## Focus
Plan, build and merge Level 4: crumbling route tiles and one-way decoys across a hall filled left / centre / right, no ferries (Level 5 will mix sinking and moving).

## What Was Built / Changed
- **Plan** (Level Roster, DESIGN §4.1/§6, MAP): Undertow, 70 × 100 m, ~110 platforms, gaps 2.6–2.8 m; crumbling = stand 1.5 s (last 0.6 s shakes) → sink 0.5 s → rise after 3 s; one-way = sinks 0.3 s after you leave, stays down. Edge cases: leaving resets the timer unless shaking; sunk tiles can't be climbed; a crumbling tile won't rise under a swimmer.
- **Server:** `sink: "crumble" | "oneway"` on platforms, validated (`src/logic/layout.ts`).
- **Generator** (`scripts/generate-level.ts`): `--spread` (soft: half the dead-end tries grow toward the emptiest third; each third must end ≥ 20%), `--crumble <share>`, `--oneway-decoys`, `--meander` (route turns from its own heading → S-curves) with an exact edge-gap solve on diagonals (`distanceForGap`). Older levels still reproduce (checked level02).
- **level04.json:** seed 3 → 110 tiles, 30-jump S-curve; then **hand-edited** by the user (+19 tiles near the start, mostly left) → 129 tiles, 19 crumbling / 25 one-way, same route; a stacked Ctrl+D copy (id 146) removed from the JSON.
- **`level:route`** rounds positions to 1 cm (Unity float noise).
- **Client:** `SinkRules` (pure state machine, 18 EditMode tests), `SinkingPlatform`, `Platform.SetClimbable`, `LevelConfig` sinking timings, `PlatformSpot.sink`, Build Level yellow (`GlowCrumble`) / red (`GlowOneway`) glows, Export keeps `sink`. User built the Level04 scene + `LevelConfig_04` (Listener glimpse every 7 s), renamed Level 3 to "Current", set its glimpse to every 5 s.
- Merged as PR #18 (commit `642ecb7`).

## Verification
- `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` **115/115** (server).
- Unity EditMode Test Runner **88/88** (`TestResults.xml`, 20:47).
- User cleared Level 4 in the Editor. **Not yet:** phone run (fps, tries, time vs par 120, do yellow/red read clearly).
- Ownership check: Q1 (crumbling tile left at 1.2 s still sinks: `SinkRules.StepCrumble` Shaking branch ignores `standing`) explained by the agent at the user's request; Q2 (why `level:route` failed after export) **skipped**.

## Mistakes & Gotchas
- **Diagonal steps were too close.** `near()` used `gap + (sizeA + sizeB)/2` as the centre distance, which only holds straight ahead; at 45° it left ~1.9 m of water. With tight 2.6–2.8 m gaps the walk could therefore only go straight (every route was exactly 20 tiles). Fix: `--meander` solves the distance for the true edge gap; old levels keep the old formula so seeds reproduce.
- **Forcing `--spread` every try stalled** (68–95 of 110 tiles fit): the emptiest patch fills up. Half-and-half fixed it.
- **Unity float noise:** Export wrote `3.5999999` for 3.6; the last jump became 2.8000031 m > 2.8 → "no way through". `level:route` now rounds to 1 cm.
- **Ctrl+D copies get new ids on Export** (127–146) and can sit on the same spot; Build Level afterwards renames them `Platform_<id>`. The validator doesn't flag stacked tiles (found by hand).
- **Level in the menu but not on the phone:** the catalog had Level 4 but `EditorBuildSettings` didn't (Update Build List not saved) — the Editor plays any open scene, the APK only listed ones.
- A test hung once: my own PowerShell glob run alongside the suite, not the level.
- A brief `.git/index.lock` from a background `git status`; retry worked, nothing deleted.
- Level 3's display name was "Shallow End" (copy of Level 1) since PR #16; fixed.

## Decisions
- Level 4 = Undertow as above (Level Roster). Level 4 stays **generated mode** (dead-end proof on) even after the hand edit — the proof still passes.
- Level 3 glimpse every **5 s** (user, more pressure); glimpse length stays 2.5 s for now (user may raise to ~4).
- C# Level 4 test checks both sink kinds parse, not exact counts (hand edits change counts).

## Next Step
Play Level 4 on the phone (Build And Run) and record lowest fps, tries, time vs par 120 and whether yellow/red tiles read clearly; then `/skill:wayfinder continue planning/still-water/MAP.md` → plan Level 5 (sinking + moving).
