# Session 2026-11-05 — Phase 2 core loop + Level 2 plan

## Focus
Lock the rest of Core Loop Rules and Save Data, build the Phase 2 level loop (win/lose, Listener, stars, save, menus), merge it through the pipeline, and plan Level 2.

## What Was Built / Changed
- **Decisions (PR #11 + this branch):** [04 Core Loop Rules](../../planning/still-water/decisions/04-core-loop-rules.md) locked (safe path, fakes from Level 2, overhead Reveal, timer, max gap 2.8 m); [11 Save Data](../../planning/still-water/decisions/11-save-data.md) locked; new [18 Level Roster](../../planning/still-water/decisions/18-level-roster.md) (levels planned one at a time; Level 2 "Deep End" locked: 40 × 50 m, ~40 platforms, gaps 2.0–2.6 m, no Reveal/fakes).
- **Level 1 reworked:** no glowing path and no Reveal (the fly-along reveal felt messy and the camera zoomed into tiles); goal text only. Regenerated as a 6-tile zig-zag route (`2 → 6 → 11 → 15 → 20 → 24`) + 18 **dead ends**; server test proves every route tile is required, even with a 3.4 m jump.
- **Server:** `safePath` validation, exit-height check, `reachesExit`, `MAX_GAP_M` 3.2 → 2.8; 36 tests.
- **Unity:** `ProgressRules` (🤝), `SaveSystem` (atomic `save.json`), `SaveData`, `LevelConfig` (SO), `LevelCatalog`, `GameState`, `LevelController` (Intro → Reveal → Crossing → Won/Lost, Paused), `ListenerAI` (Dormant → Alerted → Hunting → Catch/Returning, pale grey-box shape), `PatternRevealer` (overhead, ready for Level 2), fake platforms (sink 0.4 s, off in Level 1), `ExitDoor`, IMGUI `MainMenu` / Level Select / `LevelHud` / Result, `ProceduralAudio` placeholder sounds (`shortcut:`).
- **Editor menus:** *Set Up Level Loop*, *Create Menu Scene*.
- Commit `6a3e150`, merged as **PR #13** (`532b6d9`); deploy to the VM verified (`/health` → `532b6d9`).
- **Wrap-up tidy:** reverted an accidental 12 cm nudge in the Starter Assets Third Person `Playground.unity`.

## Verification
- Server: `npm run lint` exit 0 · `npm run typecheck` exit 0 · `npm test` **36/36**, exit 0.
- Unity Test Runner (EditMode): **40/40** (`TestResults.xml`, 2026-11-05); RED first for `ProgressRules` (15 failing) and `SaveSystem` (7 failing).
- Editor Play: Level 1 win → 3 stars → `save.json` written (17.95 s).
- GitHub: PR #13 `test` green, deploy green.
- **Not done (deferred):** phone playtest of the full loop (win, catch, timer, force-close keeps progress), Phase 1 10-jump bar + outside tester.
- Ownership check: 1 question (data flow), answer pending at wrap-up.

## Mistakes & Gotchas
- **Fly-along Reveal camera** was messy and zoomed into the tiles → Level 1 dropped the glow entirely; the overhead `PatternRevealer` waits for Level 2+.
- **Fake platforms in a tutorial** felt unfair → Level 1 uses dead ends instead (no fakes), checked by a test, not by eye.
- `ProgressRules.IsUnlocked`: needed a loop to find the level's index; `prev != null && prev.cleared` is safe because `&&` stops early.
- **GitHub billing lock** failed the `test` job in ~3 s and blocked merging (ruleset needs `test`). Cause: likely the Student plan lapsing back to Free. Public-repo Actions show a metered amount (~$0.16) that is 100% discounted; *Next payment due* stays "–".
- `git fetch` from WSL/`cmd.exe` failed with `Permission denied (publickey)`; pushes from the user's own terminal work (key/agent not loaded in the agent's shell).
- An empty root `Assets/Resources` folder (+ `.meta`) was left untracked; the real catalog is in `_Project/Resources`.

## Decisions
- [04 Core Loop Rules](../../planning/still-water/decisions/04-core-loop-rules.md): only the safe path holds you (fakes from Level 2); Level 1 = dead ends, no Reveal; timer after Reveal; max gap 2.8 m.
- [11 Save Data](../../planning/still-water/decisions/11-save-data.md): `save.json` in persistentDataPath, best-only merge, atomic write before Result, corrupt → defaults.
- [18 Level Roster](../../planning/still-water/decisions/18-level-roster.md): Level 2 locked; Level 3 planned once Level 2 is playable.
- CI/CD stays on (free for public repos; turning it off would block every merge).

## Next Step
In Unity, delete the empty `Assets/Resources` folder (Project window → right-click → Delete). Then `git switch -c feat/phase3-levels` and `/skill:execute-phase Phase 3` (level generator → `level02.json` → *Update Build List*).
