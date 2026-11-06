# Level Roster

- **Status:** Open — resolved **one level at a time** with the user (Level 2 locked 2026-11-05)
- **Type:** grill (per level)
- **Affects:** Phase 3 (and later levels), DESIGN §5.3 + §6, `server/src/levels/`, `LevelConfig_NN`
- **Replaces:** the old fixed DESIGN §6 plan (Reveal + fakes in L2, moving in L3, fog/sinking in L4, everything in L5)

## Question
What is each level, and in what order do new mechanics arrive?

## How we decide (by: me, 2026-11-05)
- The lab asks for **at least 5** levels, not exactly 5: the roster can grow past 5.
- Each level starts from **the user's own idea**; the agent turns it into concrete numbers (hall size, platform count/sizes, gaps, route length, timer, par, Listener) and the user confirms before it's built.
- Every level must be "harder or different" (lab R1). Hard limit from the jump: gaps ≤ 2.8 m.
- Mechanics not yet scheduled (assign them when the user plans a level): glowing path Reveal, fake platforms, moving platforms, timed sinking platforms, fog/flicker "lights out".

## Level 1 — Shallow End (built, Phase 2)
30 × 33 m · 24 platforms (6 route + 18 dead ends) · gaps 1.5–2.25 m · no Reveal, no fakes · Listener **8 m/s, alert 0.6 s** (user-tuned in the Editor), roams as a **Fin** (no glimpses) · timer 120 s, par 60 s.

## Level 2 — Deep End (locked 2026-11-05, retuned after the first playtest, by: me)
**User's idea:** bigger, larger, wider and harder to complete; no sinking, no glowing path.
**Playtest 1 (developer, 2026-11-05):** L1 2–4 tries, L2 2–3 tries, L2 well under par (70 s), Listener never threatening → L2 wasn't clearly harder than L1 (lab R1). The Listener was already at 11 m/s / 0 s during that playtest, so it didn't threaten because falls were rare: the difficulty has to come from the layout and the clock. Retuned: harder layout **and** tighter timer/par.

| Setting | Value (first build → retuned) |
|---|---|
| Hall | **40 × 50 m** (L1: 30 × 33) |
| Platforms | 40 → **50**: one dry route of **9 tiles (10 jumps)** + dead ends (skipping any route platform makes the exit unreachable, server-tested) |
| Platform sizes | 2 m, small **1.6 m**: 25% → **40%** |
| Gaps | 2.0–2.6 → **2.2–2.7 m** (max allowed 2.8) |
| Mechanics | same as L1: no Reveal (`revealPath` off), no fakes (`fakePlatforms` off) |
| Listener | **11 m/s, alert 0 s** (user-tuned in the Editor before playtest 1; you swim at 2.0, so falling far from an edge is nearly always a catch); roams as a **Wake** with a glimpse every ~15 s |
| Timer / par | 150 / 70 → **110 / 55 s** |
| Goal text | "Deeper and wider. Find the way across." |

**Built:** `npm run level:gen -- --id level02 --name "Deep End" --width 40 --length 42 --platforms 50 --route 8-10 --gaps 2.2-2.7 --sizes 2,1.6 --small 0.4 --seed 1 --force` → 42 m of water between 4 m decks, 50 platforms (19 × 1.6 m), route `1 → 5 → 10 → 16 → 24 → 29 → 35 → 42 → 50` (gaps 2.20–2.70 m), dead ends on both sides. At ≤ 2.7 m gaps, 42 m only fits ~10 jumps, so the route runs nearly straight; the challenge is the dead ends and small tiles.

## Level 3 — Current (locked 2026-11-05, made harder 2026-11-06 after playtest, by: me; name and goal text can change)
**User's idea:** bigger, wider, longer; platforms much more spread out; dead ends you **can't see** from where you choose, so a wrong turn means a **long, dangerous detour back**; add moving platforms.
**Playtest 1 (developer):** still very easy → bigger hall, every jump near the limit, meaner ferries, longer decoys. Gaps can't grow past **2.8 m**: the jump (run 4.5 m/s, height 1.2 m) reaches ~3 m, so difficulty comes from distance, tile size, timing and decoys.

| Setting | Value (first build → harder) |
|---|---|
| Hall | 50 × 70 → **70 × 100 m** (92 m of water between 4 m decks) |
| Platforms | 60 → **100**: a route of **19 jumps** (turns up to 70°, so "always go forward" fails) + **4–6 decoy branches of 5–9 jumps** (dead ends up to **13 jumps** from the route) + short dead ends. Same dead-end rule, server-tested |
| Gaps | 2.4–2.8 → **2.6–2.8 m**; small 1.6 m tiles 30% → **~50%**; ferry boarding gaps 2.0–2.4 m |
| Ferries | 3 → **4** route tiles across unjumpable channels: travel 4–5 → **5–6 m** in 3 → **2 s**, pause 1 → **0.5 s**. Warm edge glow. Nothing may come within **1.5 m** of a ferry's slide (server rule; after a diagonal ferry passed 0.6 m from a tile) |
| Mechanics | no Reveal, no fakes |
| Listener | roams **Hidden** (drone only) + glimpses; user-tuned in the Editor (14 m/s, alert 0 s, glimpse every 15 s); starts in the **middle of the pool** |
| Timer / par | suggested **200 / 120 s** (19 jumps at L2's pace + ferry waits + a decoy detour); tune after the playtest |
| Goal text | "Some paths lead nowhere. Ride the ferries across." |
| Lamps | **35**, about 15 m apart (Build Level caps big halls near 32 lamps for phone fps) |

**Hand-tuned (2026-11-06, by: me):** after the harder build the user edited the scene in Unity (10 copied platforms, 7 moved, incl. route tile 6) and found it clearly harder (3–4 tries). Exported back (*Export Level to JSON* → `npm run level:route -- --id level03 --max-gap 3.1 --hand-tuned`): **110 platforms**, route = the easiest way through, **26 jumps, longest 3.06 m** (left side over the copies, rejoining at ferry 39): `110 → 109 → 106 → 108 → 105 → 26 → 23 → 30 → 101 → 38 → 45 → 44 → 42 → 41 → 39F → 48 → 53 → 59 → 62F → 72 → 76 → 85 → 94 → 95F → 100`. `maxGap` 3.1 (Core Loop Rules amended); **not** one-dry-route any more (the old right-side way still exists but needs > 3.06 m jumps); ferry 13 is now a **moving decoy**. Still server-checked: a way through within 3.1 m, ferry clearance 1.5 m. The generated version below is kept for reference (`/tmp` copy not kept; regenerate with the command).

**Built:** `npm run level:gen -- --id level03 --name "Current" --width 70 --length 92 --platforms 100 --route 18-32 --gaps 2.6-2.8 --sizes 2,1.6 --small 0.5 --max-turn 70 --decoys 4-6 --decoy-length 5-9 --ferries 4-5 --ferry-travel 5-6 --ferry-gaps 2.0-2.4 --ferry-seconds 2 --ferry-pause 0.5 --seed 16 --force` → route `1 → 6 → 10 → 13F → 24 → 29 → 34 → 39F → 48 → 53 → 59 → 62F → 72 → 76 → 85 → 94 → 95F → 100` (F = ferry), 48 small tiles, every ferry ≥ 2.0 m from other tiles along its real path.

**How ferries fit the rules (by: delegated):**
- JSON: a platform may have `move: { dx, dz, seconds, pause, offset }`; its `x/z` is end A, end B = A + (dx, dz).
- Server checks treat a ferry as the **rectangle it sweeps** (A to B): the route reaches it at A and leaves at B; any decoy near *any* point of its slide would count as a shortcut, so the dead-end rule stays safe while it moves. Ferries only on the route; `seconds > 0`, `pause ≥ 0`, sweep inside the water, 1.5 m clearance.
- Riding: the player is carried by the ferry's movement each frame (`PlatformMover` runs first, `PlayerMotor` adds its `Delta`). Falling off = a normal splash. You can climb onto a ferry from the water like any route tile.

## Level 4+
_(plan with the user when Level 3 is playable)_
