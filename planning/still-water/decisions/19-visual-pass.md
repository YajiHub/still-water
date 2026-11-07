# Visual Pass

- **Status:** Locked (2026-11-06) (by: me): scope = Listener + water + pool hall look, **before Level 5**
- **Type:** grill
- **Blocks:** Level 5 build ([Level Roster](./18-level-roster.md)); pulls part of Phase 5 art forward

## Question
Levels 1–4 are all grey-box (primitive Listener, flat water, plain tiles). Which visuals must improve before more levels, so playtests reflect the real game?

## Why it matters
The scare and the "don't splash" tension can't be judged with a capsule monster and flat water; tuning levels against grey-box risks retuning everything later.

## Options (pick any)
| Part | Gain | Cost |
|---|---|---|
| Listener model + animations | Biggest scare upgrade; glimpses read as a creature | Needs Monster Identity + an asset source; ~1 session |
| Water shader + splash/ripples | Sells the core mechanic | Shader Graph + particles; ~1 session |
| Pool hall look (tiles, walls, lamps) | Mood | CC0 textures/models; ~1 session |
| Catch moment (camera + sound) | Losing feels scary | ~1 session |

## Decision
**Phase 3b: Visual Pass**, before Level 5, covering:
1. **Listener model + animations** (glimpse/swim, surfacing, catch reveal)
2. **Water shader + splash / ripples**
3. **Pool hall look:** tiled textures, walls, lamp fixtures

Not in this pass: the scripted **catch moment** (camera + sound) → Phase 5 (Fog of War).

## Constraints (carried)
- ₱0; every asset licensed and in [CREDITS.md](../../../docs/CREDITS.md) in the same commit; no CC-BY-NC; nothing resembling the Roblox inspiration's crab monster.
- Art Style C (fog, dim cold light, grain, render scale 0.6) stays; assets must read in low light; edge glow stays the main readability cue.
- Phone: 30 fps minimum with the 35 hall lamps (Level 3/4 halls) — measure before/after.
- Grey-box Build Level keeps working: visuals swap in via materials/prefabs referenced by `LevelBuilder`, not by hand-editing scenes.

## Opens next
[Title and Monster Identity](./06-title-and-monster.md) (the monster half), then Listener asset source, water look, hall assets.
