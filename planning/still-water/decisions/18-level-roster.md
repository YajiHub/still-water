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
| Listener | roams **Hidden** (drone only) + glimpses; user-tuned in the Editor (14 m/s, alert 0 s, glimpse every 15 s → **5 s** on 2026-11-06, user's choice: more pressure; each glimpse lasts 2.5 s); starts in the **middle of the pool** |
| Timer / par | suggested **200 / 120 s** (19 jumps at L2's pace + ferry waits + a decoy detour); tune after the playtest |
| Goal text | "Some paths lead nowhere. Ride the ferries across." |
| Lamps | **35**, about 15 m apart (Build Level caps big halls near 32 lamps for phone fps) |

**Hand-tuned (2026-11-06, by: me):** after the harder build the user edited the scene in Unity (10 copied platforms, 7 moved, incl. route tile 6) and found it clearly harder (3–4 tries). Exported back (*Export Level to JSON* → `npm run level:route -- --id level03 --max-gap 3.1 --hand-tuned`): **110 platforms**, route = the easiest way through, **26 jumps, longest 3.06 m** (left side over the copies, rejoining at ferry 39): `110 → 109 → 106 → 108 → 105 → 26 → 23 → 30 → 101 → 38 → 45 → 44 → 42 → 41 → 39F → 48 → 53 → 59 → 62F → 72 → 76 → 85 → 94 → 95F → 100`. `maxGap` 3.1 (Core Loop Rules amended); **not** one-dry-route any more (the old right-side way still exists but needs > 3.06 m jumps); ferry 13 is now a **moving decoy**. Still server-checked: a way through within 3.1 m, ferry clearance 1.5 m. The generated version below is kept for reference (`/tmp` copy not kept; regenerate with the command).

**Built:** `npm run level:gen -- --id level03 --name "Current" --width 70 --length 92 --platforms 100 --route 18-32 --gaps 2.6-2.8 --sizes 2,1.6 --small 0.5 --max-turn 70 --decoys 4-6 --decoy-length 5-9 --ferries 4-5 --ferry-travel 5-6 --ferry-gaps 2.0-2.4 --ferry-seconds 2 --ferry-pause 0.5 --seed 16 --force` → route `1 → 6 → 10 → 13F → 24 → 29 → 34 → 39F → 48 → 53 → 59 → 62F → 72 → 76 → 85 → 94 → 95F → 100` (F = ferry), 48 small tiles, every ferry ≥ 2.0 m from other tiles along its real path.

**How ferries fit the rules (by: delegated):**
- JSON: a platform may have `move: { dx, dz, seconds, pause, offset }`; its `x/z` is end A, end B = A + (dx, dz).
- Server checks treat a ferry as the **rectangle it sweeps** (A to B): the route reaches it at A and leaves at B; any decoy near *any* point of its slide would count as a shortcut, so the dead-end rule stays safe while it moves. Ferries only on the route; `seconds > 0`, `pause ≥ 0`, sweep inside the water, 1.5 m clearance.
- Riding: the player is carried by the ferry's movement each frame (`PlatformMover` runs first, `PlayerMotor` adds its `Delta`). Falling off = a normal splash. You can climb onto a ferry from the water like any route tile.

## Level 4 — Undertow (locked 2026-11-06, by: me; name and goal text can change)
**User's idea:** sinking platforms, but no moving ones (Level 5 will mix sinking + moving + more). Platforms spread **left, centre and right**, not bunched on one side.

| Setting | Value |
|---|---|
| Hall | **70 × 100 m** (like L3), **filled across the whole width**: each third (left / centre / right) holds ≥ 20% of the platforms (generator + test) |
| Platforms | **~110**: a winding route (~20–25 jumps, turns up to 70°) + decoy branches (5–9 jumps) + short dead ends; one-dry-route proof on (generated, not hand-tuned) |
| Gaps | **2.6–2.8 m** (default max 2.8: the sinking adds the pressure, not the jump length) |
| **New: crumbling** | **~half the route tiles**: stand on one **1.5 s** → it shakes → sinks over **0.5 s** → rises **3 s** later. Leaving resets the timer, unless it's already shaking. Pale yellow glow |
| **New: one-way** | **decoy tiles**: sinks **0.3 s** after you jump off, **stays down** for the attempt. A wrong turn = the way back is gone, you swim (Listener). Red glow |
| Mechanics | no Reveal, no fakes, no ferries |
| Listener | roams **Hidden** + glimpses; speed/alert user-tuned in the Editor (L3: 14 m/s, 0 s) |
| Timer / par | provisional **200 / 120 s**; tune after the playtest |
| Goal text | "Don't stand still. Some paths close behind you." |

**Built:** `npm run level:gen -- --id level04 --name "Undertow" --width 70 --length 92 --platforms 110 --route 24-36 --gaps 2.6-2.8 --sizes 2,1.6 --small 0.4 --max-turn 85 --meander --decoys 4-6 --decoy-length 5-9 --spread --crumble 0.5 --oneway-decoys --seed 3` → 110 platforms, route of **29 tiles (30 jumps)** sweeping right → centre → left wall, gaps 2.60–2.80 m, **15 crumbling** route tiles, **23 one-way** decoy tiles, left / centre / right **31 / 36 / 43**, deepest dead end 11 jumps. **Then hand-edited** (2026-11-06): +19 tiles near the start, mostly left (129 total, 19 crumbling / 25 one-way; a stacked Ctrl+D copy, id 146, removed from the JSON), exported, `level:route` → same 30-jump route, dead-end proof still passes (not hand-tuned mode). (`--meander` was needed: with tight 2.6–2.8 gaps the old walk could only go straight.)

**Rules (by: delegated):**
- Which tiles: JSON `sink: "crumble" | "oneway"` per platform (layout data); the timings live in `LevelConfig` (tuning): crumble stand 1.5 s, sink 0.5 s, rise after 3 s, one-way delay 0.3 s.
- Server checks are unchanged: every tile exists at the start, so the dead-end proof still holds. `sink` must be one of the two values.
- A sunk tile is not climbable. A crumbling tile **waits to rise while a swimmer is right above it** (no popping you up); it rises once you've moved off.
- Retry reloads the scene, so every tile is back.

## Level 5 — The Drain (locked 2026-11-07, by: me; name and goal text can change)
**User's idea:** the finale mixes what came before: moving (L3) + sinking (L4). No new mechanics (lights out, fakes + Reveal stay unused; Pattern Re-show stays blocked).

| Setting | Value |
|---|---|
| Hall | **70 × 100 m**, filled **left / centre / right** (each third ≥ 20%, like L4) |
| Platforms | **~120**: S-curve route of **~30 jumps** (meander) + decoy branches (5–9 jumps) + short dead ends; one-dry-route proof on (generated) |
| Gaps | **2.6–2.8 m** (ferry gaps as on L3) |
| Ferries (L3) | **2–3**, mid-route, across channels you can't jump |
| Crumbling (L4) | **~half the route tiles** (never a ferry); a crumbling tile right after a ferry is allowed: that's the finale's squeeze |
| One-way (L4) | **decoy tiles** |
| Listener | roams **Hidden** + glimpses, L4 speed (user-tuned in the Editor) |
| Timer / par | provisional **220 / 140 s**; tune after the playtest |
| Goal text | "Everything you've learned. Get out." |

**Built:** `npm run level:gen -- --id level05 --name "The Drain" --width 70 --length 92 --platforms 120 --route 26-38 --gaps 2.6-2.8 --sizes 2,1.6 --small 0.4 --max-turn 85 --meander --decoys 4-6 --decoy-length 5-9 --spread --crumble 0.5 --oneway-decoys --ferries 2-3 --ferry-travel 5-6 --ferry-gaps 2.0-2.4 --ferry-seconds 2 --ferry-pause 0.5 --seed 3` → 120 platforms, route of **31 tiles (32 jumps)**: left → ferry up the left → two ferries across the middle → up the right wall → across the top; gaps 2.12–2.80 m (ferry gaps 2.0–2.4), **3 ferries** (8, 26, 35), **16 crumbling**, **31 one-way**, left / centre / right **37 / 44 / 39**, deepest dead end 13 jumps. All 16 probed seeds generated (the ferry + spread + meander mix works).

**Rules (by: delegated):** no new rules: ferries, crumbling and one-way behave exactly as on L3/L4 (same code, same `LevelConfig` timings). Check while generating: ferries + `--spread` + `--meander` together (never combined before); if the walk can't fit ferries on the meandering route, draw the map for the user and adjust before building.

## Level 6+
_(post-lab ideas: lights out, fakes + the Reveal; see [IDEAS.md](../../../docs/IDEAS.md))_
