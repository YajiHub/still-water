# Session 2026-11-05/06 — Phase 3: Levels 2 and 3, roaming Listener, dark-look readability

## Focus
Build Phase 3 one level at a time: the level generator, Level 2 "Deep End" (and its retune), the roaming Listener, readability in the dark look, and Level 3 "Current" with decoy branches and ferries, which ended up hand-tuned by the user.

## What Was Built / Changed
- **Level generator** `npm run level:gen` (`server/scripts/generate-level.ts`): seeded; route walk + short dead ends; later **decoy branches** (forward-heading dead-end chains), **ferries** (`move` on route tiles), `--max-turn`, `decoyDepth`. Level JSON flag `"oneDryRoute"`: the dead-end test runs for every flagged level.
- **Server rules** (`layout.ts`): ferries = swept rectangle for reach/skip checks; ferries only on the route (one-dry-route levels); **1.5 m clearance** around a ferry's slide; per-level **`maxGap`** (default 2.8, cap **3.1** `JUMP_LIMIT_M`); **`easiestRoute`** (path whose longest jump is shortest).
- **Hand-edit workflow:** Unity *Still Water → Export Level to JSON* → `npm run level:route -- --id levelNN [--max-gap 3.1] [--hand-tuned]` → *Build Level*.
- **Level 2 Deep End:** 40 × 50 m; retuned after playtest 1 to 50 platforms, 40% small tiles, gaps 2.2–2.7, timer 110 / par 55 (PR #14, #15).
- **Level 3 Current:** first 50 × 70 m, then 70 × 100 m / 100 platforms / 4 fast ferries (2 s, 0.5 s pause), then **hand-tuned by the user** → 110 platforms, 26-jump route, longest 3.06 m, maxGap 3.1, ferry 13 a moving decoy (PR #16).
- **Unity:** `ListenerRules` + roaming `ListenerAI` (Roaming → Alerted → Hunting → Lingering; never teleports; roam modes Fin / Wake / Hidden + glimpses; 3D drone; starts mid-pool); `FerryRules`, `PlatformMover` (execution order −100), `PlayerMotor` rides ferries via `GroundPlatform`; *Update Build List* menu; warm `GlowFerry`; `LevelLayout.move/maxGap/oneDryRoute`.
- **Dark look (C) readability:** runtime headlamp + Listener skin/wake glow from `LookProfile`; lamp grid every ~12 m (capped near 32 lamps for huge halls); Mobile renderer → **Forward+**.
- **Docs:** Level Roster (L1–L3), Core Loop Rules (roaming Listener, maxGap), DESIGN §5.2/§5.3/§6, Art Style (readability), Grey-box Build Method (hand-edit flow), CONVENTIONS ("steps are never vague").

## Verification
- Server: `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` **77/77** (fewer than the 95 peak because level03's dead-end proofs are off: hand-tuned).
- Unity Test Runner (EditMode): **69/69** (`TestResults.xml`, 2026-11-06 17:36). RED seen first for `ListenerRules` (10/57 failing) and ferry/clearance/maxGap server tests; `FerryRules` RED skipped (user asked the agent to write the body; math checked 11/11 in a Python copy first).
- Phone: Level 2 in Look C **55–60 fps** (12 lamps, Forward+). Level 3 at 35 lamps: user said "good", no fps number recorded.
- Playtests (developer): L1 2–4 tries; L2 2–3 tries → retuned → ★★★ in 43 s (par 55); L3 generated = "very easy" → harder → hand-tuned: 3–4 tries.
- Ownership check (2 questions): ferry ride data flow: user asked for the explanation (agent answered); "Build Level on an edited scene": right (edits replaced), missing the why (it rebuilds from the JSON, the source of truth).

## Mistakes & Gotchas
- **Straight routes:** 42–62 m of water in ~10–14 jumps of ≤ 2.8 m forces a straight line, so "always go forward" beats every decoy. Fix: more jumps + turns up to 70°.
- **Seeds can fail** ("try another seed"): normal for a rejection-sampling generator; pick from several and draw them as ASCII maps.
- **Reproducibility:** new random draws changed old seeds; kept the exact random order when ferries/decoys are off, so level02 still regenerates from its documented command.
- **Diagonal ferry** passed 0.6 m from a tile: the route spacing check skipped the tile right before the new one. Fixed in the generator + a server clearance rule (written test-first).
- **Hand edits drift from the JSON:** the user's Level 3 edits needed 3.06 m jumps (over the 2.8 rule), moved a route tile, made ferry 13 a decoy, and broke one-dry-route; nothing checked it until the export. Build Level on an edited scene would have erased it.
- **Platforms can't go further apart:** the jump (4.5 m/s, 1.2 m) reaches ~3 m, so difficulty comes from distance, small tiles, timing and decoys.
- **JsonUtility fills a missing nested class with zeros** → `PlatformSpot.Moves` checks for real movement.
- **Builder reuses assets:** new `LookProfile` fields didn't reach the existing `Look_C` → one-time upgrade in *Set Up Looks*.
- **URP Forward lights each object with ≤ 4 extra lights** (the big water plane too) → Forward+ for more lamps.
- **Water is opaque:** "faintly visible underwater" is impossible; only what breaks the surface shows (fin, wake).
- **Listener far-corner start** made the first fall safe → now starts mid-pool.
- **User-tuned Listener speeds** (L1 8, L2 11, L3 14 m/s, alert 0–0.6 s): a fall is nearly always a catch, so the ★★ "≤ 1 splash" tier is almost unreachable on L2/L3. Not changed; worth a look in Phase 5.
- `git fetch` from WSL fails (`Permission denied (publickey)`, key not loaded in the agent shell); a background `git diff` briefly held `.git/index.lock` during the user's commit.

## Decisions
- [18 Level Roster](../../planning/still-water/decisions/18-level-roster.md): Level 2 Deep End (retuned), Level 3 Current (decoys, ferries, Hidden Listener, hand-tuned).
- [04 Core Loop Rules](../../planning/still-water/decisions/04-core-loop-rules.md): roaming Listener (never teleports, per-level roam mode, mid-pool start); per-level `maxGap` ≤ 3.1.
- [08 Art Style](../../planning/still-water/decisions/08-art-style.md): dark-look readability (headlamp, lamp grid, Listener glow, Forward+).
- [16 Grey-box Level Build Method](../../planning/still-water/decisions/16-grey-box-level-build-method.md): hand-edit → export → route → rebuild.
- Real graphics stay in Phase 5; builder menus carry them to every level, so the level count doesn't block art.

## Next Step
`/skill:wayfinder continue planning/still-water/MAP.md` → plan **Level 4** from the user's idea (unused so far: glowing-path Reveal, fake platforms, timed sinking platforms, lights out). Any time: Level 3 fps on the phone, an outside tester (Phase 1 exit bar), the Phase 2 phone loop evidence (R3–R5).
