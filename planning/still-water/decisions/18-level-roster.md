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

## Level 3+
_(plan with the user when Level 2 is playable)_
