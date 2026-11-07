# 2026-11-07 — Level 5 "The Drain" (planned and built)

## Focus
The user set the goal "complete up to 5 levels". Committed Phase 3b, planned Level 5 as the finale mixing earlier mechanics, generated it, and the user built the scene.

## What Was Built / Changed
- **Phase 3b** committed on `feat/visual-pass-3b` (`99b9281`) + Level 5 plan (`59e85ce`); user pushes / PRs it. Water arc **deferred** (by: me).
- **Plan** ([Level Roster](../../planning/still-water/decisions/18-level-roster.md), DESIGN level table): The Drain = ferries (L3) + crumbling (L4) + one-way decoys (L4), 70 × 100 m spread, ~120 platforms, ~30 jumps, Listener Hidden, timer 220 / par 140 (provisional). Lights out and fakes + Reveal left for 6+.
- **level05.json** (branch `feat/level5-drain`): seed 3 → 120 platforms, 31-tile route (32 jumps: left → ferry → two ferries across the middle → right wall → top), 3 ferries, 16 crumbling, 31 one-way, L/C/R 37/44/39, deepest dead end 13. No generator code changes needed.
- **User (Editor):** Level05 scene built; `LevelConfig_05` = The Drain, goal text, no fakes/Reveal, 220 / 140, Hidden, glimpse every **6**, hunt speed **20** (user's values, harder than L4's 7 / 18); build list = Menu + 5 levels. `Level04.unity` re-saved (114 lines changed, Editor serialization).

## Verification
- `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` **147/147** (Level 5 adds 32 dead-end proof tests).
- Generator mix (ferries + spread + meander + crumble + one-way) probed on 16 seeds: all generated.
- Unity EditMode: last run still **88/88 (11-06)**; not run on Phase 3b / Level 5 (expect 96).
- Not yet: Level 5 played in the Editor or on the phone; phone fps after the visual pass.
- Ownership check skipped (no code changed this session: level data + scenes only).

## Mistakes & Gotchas
- WSL has no SSH key for GitHub (`Permission denied (publickey)` on `git fetch`): the agent can't see whether PRs merged; ask the user.
- Level 5 branched from the unmerged `feat/visual-pass-3b`: its PR shows only Level 5 commits once 3b is merged; merge 3b first.

## Decisions
- Level 5 = The Drain as above (by: me); no new mechanics (by: me).
- Listener on L5 a notch harder than L4: glimpse every 6 s, hunt speed 20 (by: me, in the Editor).

## Next Step
Merge the `feat/visual-pass-3b` PR (Test Runner first: expect 96/96), then play Level 5 in the Editor and on the phone: tries, time vs par 140, lowest fps on L4 and L5 (≥ 30) → tune, then commit `feat/level5-drain` via PR.
