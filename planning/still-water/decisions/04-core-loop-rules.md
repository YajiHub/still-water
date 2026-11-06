# Core Loop Rules

- **Status:** Locked (2026-11-05) (by: me), except **re-showing the pattern**, moved to [Pattern Re-show](./17-pattern-reshow.md)
- **Type:** domain-model
- **Blocks:** Phase 2

## Question
What are the exact rules and states of one level (reveal, crossing, splash, catch, timer, stars)?

## Why it matters
These rules decide the `LevelController` state machine, the HUD, star logic, and what the online server has to copy. Validate the edge cases before coding.

## Draft model

### Level states
```
Intro(goal text) → Reveal → Crossing ⇄ InWater → Won | Lost
                                   └─ Paused (from any playable state)
```

### Player states
`Grounded → Airborne → Grounded` · `Airborne/Grounded(on fake) → InWater → Grounded (climbed)` · `InWater → Caught`

### Invariants
- The timer starts when Reveal ends (not during the reveal).
- Splash count +1 each time the player enters the water.
- Every splash strengthens the Listener (+10% speed for the rest of the level).
- Fake platforms sink ~0.4 s after the player lands; they stay sunk for the rest of the attempt.
- Win is checked on touching the Exit trigger; saving happens before the Result screen.

## Edge cases to validate
1. Can the player walk forward during the Reveal, or are they frozen? *(Proposed: frozen.)*
2. Timer: on every level, or only some? What happens at 0, an instant loss? *(Proposed: all levels; 0 = loss.)*
3. Re-show the pattern: allowed? Cost? *(Proposed: once, and you lose the ★★★ chance.)*
4. Climb-out spot: the nearest safe platform, or only the last one you stood on? *(Proposed: any safe platform you swim to.)*
5. Can you swim the whole way to the exit? *(Must be prevented: swim speed < monster speed, and the exit has a tall edge you can't climb from the water.)*
6. Does landing on a fake platform reveal it permanently (so a retry is easier)? *(Proposed: retry uses the same layout; the player's memory improves, which is fine.)*
7. Does the safe path reshuffle between attempts? Options: never (fixed per level) / on every retry / only when you leave and come back. *(Proposed: fixed per level for the lab. That makes difficulty predictable for playtests. Reshuffling is a post-lab idea.)*

## Resolved early (2026-11-04) (by: me)
- **Water is deep.** You can't touch the bottom; you swim **at the surface** (head above water), no wading and no diving. Swim speed from `MovementConfig` (Draft 2.0 m/s). Diving is a post-lab idea.
- **Edge case 4, climb-out:** swim to the edge of **any safe platform** you reach and press Jump to climb up. Sunk fake platforms can't be climbed. The Exit edge is too tall to climb from the water (blocks swimming to the exit, edge case 5).

## Decision (2026-11-05) (by: me)

### Level states
```
Intro (goal text, 2 s) → Reveal (overhead camera) → Crossing ⇄ player in water → Won | Lost
Paused from any state: freezes everything, timer included
```

### Rules
| # | Rule | Locked answer |
|---|---|---|
| — | Which platforms hold you | **Only the safe path**, on levels with fakes switched on: every other platform is fake. **Level 1 has no fakes** (amended 2026-11-05, by: me): it introduces the reveal, jumping, splash and Listener. It also has **no Reveal** (amended again 2026-11-05 after the Editor test: a fly-along reveal felt messy): goal text only, *"Get to the door without falling into the water."* The level that first uses the glowing path and fakes is chosen in the [Level Roster](./18-level-roster.md) (`revealPath` + `fakePlatforms` in `LevelConfig`). Level 1's other platforms are **dead ends**: one dry route to the exit (server test: skipping any path platform makes the exit unreachable, even with a generous 3.4 m jump). A wrong turn means swimming back (splash → Listener). Fakes start in Level 2. Per-level switch `fakePlatforms` in `LevelConfig`. The path is a list of platform ids in the level JSON (`safePath`); the server checks each step is jumpable. |
| — | Pattern length | **= the path's length** (not a separate number), so the layout and the reveal can't disagree. |
| 1 | Move during the Reveal | Frozen: controls off until the pattern ends. |
| — | How the Reveal is seen (first person) | The camera lifts to an **overhead view** behind the start (~8 m, in config), the path glows tile by tile with the rising chime, then the camera glides back into the player's eyes and controls unlock. |
| 2 | Timer | On every level; starts when the Reveal ends; 0 = **lose**. |
| 3 | Re-show the pattern | **Open:** see [Pattern Re-show](./17-pattern-reshow.md). |
| 6/7 | Path between tries | **Fixed per level** (offline). Online uses a seed (Netcode Model). |
| 8 | Landing on a fake | Sinks after **0.4 s**; jumping off in time saves you. Stays sunk for the rest of the attempt. |
| 9 | Caught while climbing | Safe once CLIMB starts (the climb takes 0.35 s). |
| — | Longest jump | **2.8 m** by default (`MAX_GAP_M`). **Amended 2026-11-06 (by: me):** a level may raise it with `maxGap`, never above **3.1 m** (`JUMP_LIMIT_M`, the edge of the jump: run 4.5 m/s, height 1.2 m). Level 3 uses 3.1 (hand-tuned). |
| 10 | Splash | +1 splash count; the Listener alerts after the level's delay (L1 1.5 s), hunts at its speed (L1 3.5 m/s), **+10% speed per extra splash** this attempt. **Amended 2026-11-05 (by: me):** it is always in the pool and never teleports: roams → hears → swims to the sound, tracking you while you swim → lingers where it last heard you → roams on from there; per-level roam mode Fin / Wake / Hidden + glimpses ([DESIGN §5.2](../../../docs/DESIGN.md)). |
| 11 | Retry | Full restart: Intro → Reveal again, fakes reset. |
| — | Stars | ★ clear · ★★ ≤ 1 splash · ★★★ 0 splashes **and** under par time (par per level, in config). |
| — | Win | Touch the exit door trigger → **save** → Result screen. |

### Level scale (for Phase 3)
Level 1 stays as built (30 × 33 m, 25 platforms, gaps 1.5–2.25 m: the teaching level). Each later level is **bigger, sparser and longer** (hall size, path length and gap sizes all grow). **Hard limit:** with the current jump (4.5 m/s, 1.2 m) the reach is ~3 m, so no gap over **2.8 m**. DESIGN §3.3's old "up to 3.2 m" is unreachable; `server/src/logic/layout.ts` `MAX_GAP_M` is 2.8 (lowered 2026-11-05 with `safePath`).

## Consequences
- Level JSON gains `safePath` (ids, start → exit order); the layout validation adds: path ids exist, start → first, each step, and last → exit are ≤ max gap.
- New Phase 2 pieces: `LevelController` (states above), `PatternRevealer` (overhead camera + glow), fake-platform sinking, `ListenerAI` (grey-box shape), HUD (timer, splashes), Result screen.
- First-person Listener audio (3D splash + heartbeat) from [Camera Style](./05-camera-style.md) consequences lands in Phase 2.
