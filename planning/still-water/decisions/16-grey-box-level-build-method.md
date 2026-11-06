# Grey-box Level Build Method

- **Status:** Locked (2026-11-04) (by: me)
- **Type:** grill
- **Affects:** Phase 1 (Level 1 grey box), Phase 3 (Levels 2–5), Phase 4 (server reads the same layouts)
- **Confirms:** the Draft "shared level data" note in [SYSTEM_ATLAS §2](../../../docs/SYSTEM_ATLAS.md#2-repository-layout-planned)

## Question
Hand-place Level 1 in the Editor, or generate it from layout data that the server can read too?

## Options
| Option | For | Against |
|---|---|---|
| **Layout JSON + Editor build script** | One source of truth for client and server; tune gaps by editing numbers; no scene-YAML hand edits | ~half a day to set up |
| Hand-place in the Editor | Fastest and most visual | Positions must be exported or retyped for Phase 4; tuning = moving cubes |
| Hand-place now, export later | Visual now | Extra exporter; the scene becomes the source of truth |

## Decision
**Hand-editing (2026-11-06, by: me):** move/copy/resize platforms in the scene → *Still Water → Export Level to JSON* (copies get new ids, ferries keep their move) → `npm run level:route -- --id levelNN [--max-gap 3.1] [--hand-tuned]` (route = easiest way through; `--hand-tuned` turns off the one-dry-route proof) → *Build Level* again. The JSON stays the one source of truth: never re-run *Build Level* on an edited scene before exporting.

**Layouts are JSON files in `server/src/levels/` (e.g. `level01.json`; made with `npm run level:gen` since Phase 3; a platform may carry `move` for ferries, Level 3). A Unity Editor menu item, *Still Water → Build Level*, builds the grey-box objects from them.**

- **Shape (v1, Draft until Phase 2):** `id`, `name`, `water` (height, bounds), `start` and `exit` (position + size), `platforms[]` (`id`, `x`, `z`, `size`, optional `type` and `path` for moving ones later). All metres, Unity axes (x/z floor, y up).
- **Editor script** (`_Project/Scripts/Editor/LevelBuilder.cs`, editor-only): reads the JSON from `../server/src/levels/` at edit time, replaces a `Level` root object in the open scene, and copies the JSON into `_Project/Configs/Levels/` as a `TextAsset` for runtime use (the phone can't read the server folder). The human runs the menu item and saves the scene.
- **One parser per side:** C# `LevelLayout` (plain class, `JsonUtility`) and TS `server/src/logic/layout.ts`. A server `node:test` validates every layout file (unique ids, sizes > 0, exit reachable by gap ≤ the config's max jump distance), so a broken layout turns CI red.
- Which platforms are *safe* (seed) is **not** in the file; that is [Core Loop Rules](./04-core-loop-rules.md) / [Netcode Model](./10-netcode-model.md).

## Why
Phase 4's server must check splashes and finishes against the same positions the phone shows. A shared file avoids two copies drifting apart, and the CI validation gives Lab 6's pipeline a real game-data check.

## Consequences
- The copied `TextAsset` must be refreshed by re-running *Build Level* after a layout change (the menu item does both).
- Art pass later replaces grey-box cubes inside the builder (prefab per platform type), not by hand.
