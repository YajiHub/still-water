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
30 × 33 m · 24 platforms (6 route + 18 dead ends) · gaps 1.5–2.25 m · no Reveal, no fakes · Listener 3.5 m/s, alert 1.5 s, roams as a **Fin** (no glimpses) · timer 120 s, par 60 s.

## Level 2 — Deep End (locked 2026-11-05, by: me)
**User's idea:** bigger, larger, wider and harder to complete; no sinking, no glowing path.

| Setting | Value |
|---|---|
| Hall | **40 × 50 m** (L1: 30 × 33) |
| Platforms | **~40**: one dry route of **~10 jumps** + dead ends (same rule as L1: skipping any route platform makes the exit unreachable, server-tested) |
| Platform sizes | mostly 2 m, some **1.6 m** |
| Gaps | **2.0–2.6 m** (L1: 1.5–2.25) |
| Mechanics | same as L1: no Reveal (`revealPath` off), no fakes (`fakePlatforms` off) |
| Listener | 3.8 m/s, alert 1.2 s, roams as a **Wake** with a glimpse every ~15 s |
| Timer / par | 150 s / 70 s |
| Goal text | "Deeper and wider. Find the way across." |

**Built (Phase 3):** `npm run level:gen -- --id level02 --name "Deep End" --width 40 --length 42 --platforms 40 --route 8-10 --gaps 2.0-2.6 --sizes 2,1.6 --small 0.25 --seed 5` → 42 m of water between 4 m decks, 40 platforms (14 × 1.6 m), route `1 → 6 → 10 → 16 → 18 → 23 → 26 → 32 → 37 → 40` (11 jumps, gaps 2.05–2.55 m). At 2.6 m max gap, 42 m only fits ~11 jumps, so the route runs nearly straight; the challenge is the dead ends branching on both sides.

## Level 3+
_(plan with the user when Level 2 is playable)_
