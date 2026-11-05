# MAP — Still Water (Wayfinder, Map Mode)

> Index, not a store: one line per item, with details in `decisions/`. Overwrite in place; never append status history.

## Phase Progress

| Phase | Scope | Blocking Decisions | Status |
|---|---|---|---|
| 0. Decide & set up | ✅ Unity + Android, repo on GitHub, Orientation locked, `client` project (Android, Player settings), Starter Assets touch controls on the phone (First Person Playground), Colyseus hello room joined from Editor + phone (LAN `ws://192.168.1.9:2567`). server lockfile on registry.npmjs.org, QA profile. Third Person Playground on the phone (Colyseus SDK embedded in `client/Packages/io.colyseus.sdk` to drop a broken upstream symlink). Remaining: GDD + paper-prototype photo (Checkpoint 1). Checklist: [SETUP.md](../../docs/SETUP.md). *(Lab checkpoints 1–2: GDD approved, repo runs)* | — | 🟡 In Progress |
| 0b. CI/CD pipeline (Lab 6) | ✅ Server `/` + `/health`, lint/typecheck/`node:test` (13 tests), Docker image, `ci-cd.yml` test → deploy on the self-hosted VM runner (VMware, Ubuntu 26.04.1), ruleset `protect-main`, Tests A/B/C, 5 deploy timings ([results](../../docs/LAB6_REQUIREMENTS.md#results-table-part-75-fill-in-during-testing)), phone reaches `:8080`. Screenshots in `docs/screenshots/` (CP2, CP5, Test B, C-M1, C-M2); CP1, CP3, CP4, CP6, Test A, C-bad still to take (any time, from the Actions history). README + paper: Phase 6. | — | ✅ Done |
| 1. Feel prototype | Grey-box Level 1 built from `level01.json`; own `PlayerMotor` (jump aids, deep-water swim, climb-out); touch UI (floating stick, drag-to-look, Jump/Climb); first vs third person rigs; 1-day art spike. **Exit bar:** on the phone, 10 jumps in a row with ≤1 miss; misses feel like the player's fault; 1 new tester clears L1 in ≤3 tries unaided; steady 30+ fps; Camera Style + Art Style locked. | ~~[Camera Style](./decisions/05-camera-style.md)~~ ✅ first person, ~~[Art Style](./decisions/08-art-style.md)~~ ✅ C. Hybrid | 🟡 In Progress: built + phone-verified; left: 10-jump bar, 1 outside tester (deferred, can run during Phase 2) |
| 2. Core loop + progression | Reveal, safe/fake platforms, splash → Listener → catch, win/lose, stars, GameState, save, Level Select with locks, Result screen | ~~[Core Loop Rules](./decisions/04-core-loop-rules.md)~~ ✅, ~~[Save Data](./decisions/11-save-data.md)~~ ✅ | 🟡 Merged (PR #13) + Editor-verified (win → stars → save); left: phone playtest (deferred) |
| 3. Levels 2–5+ + offline | At least 5 levels, **planned one at a time with the user** ([Level Roster](./decisions/18-level-roster.md)); Level 2 = bigger/harder, same rules. Later levels may add the Reveal, fakes, moving/sinking platforms, lights out. Optional Ghost rival; airplane-mode test. *(Checkpoints 3–4)* | [Level Roster](./decisions/18-level-roster.md) (per level), [Offline Rival](./decisions/09-offline-rival.md) | 🟡 Levels 1–2 done (L2 retuned after playtest; roaming Listener; dark-look readability; 55–60 fps); next: plan Level 3 |
| 4. Online | Colyseus RaceRoom, NetClient, lobby (quick match + room code), interpolation, server-decided result, error/disconnect handling, logic tests for every server rule, deployed by the pipeline to the VM (LAN), optional Render for internet play. *(Checkpoint 5)* | [Online Match Format](./decisions/07-online-match-format.md), [Netcode Model](./decisions/10-netcode-model.md) | [ ] Pending |
| 5. UI, audio, polish, builds | All screens, pause, settings, credits, sound, juice, icon; APK on 2+ phones; test matrix. *(Checkpoints 6–7)* | [Title and Monster Identity](./decisions/06-title-and-monster.md) | [ ] Pending |
| 6. Playtest & submit | Lab 5: 5+ testers, analysis, ≥ 3 fixes + retest, README, release `lab5-v1.0`, demo video, IEEE paper. Lab 6: results table (≥ 5 deploy timings), release `lab6-v1.0`, its own IEEE paper. *(Checkpoint 8, final)* | — | [ ] Pending |

**Time budget (Draft):** core build in ~1–2 weeks (Phases 0–5), then playtest and paper inside the lab's 5-week window. Rough split: P0 1 day · P0b ~1 day (Lab 6 is about 6 h) · P1 2 days · P2 2–3 days · P3 2 days · P4 3 days · P5 2 days.

## Roadmap — tagged tasks (🧑 me · 🤝 together · 🤖 agent)
> Only the phases that can be specified now. Later phases get tasks when their decisions are resolved.
> Step details: [SETUP.md](../../docs/SETUP.md). Retag anything you like.

### Phase 0 — Decide & set up
- [x] 🧑 Resolve [Orientation](./decisions/03-orientation.md) (wayfinder grill)
- [x] 🤖 `git init -b main` + repo Git settings (`core.autocrlf=false`, `core.fileMode=false`) — SETUP §1
- [x] 🧑 Create the Unity project `client` (Universal 3D, 6000.3.27f2) in Unity Hub — SETUP §2
- [x] 🤖 Add Unity's `.gitignore` inside `client/`; confirm Visible Meta Files + Force Text — SETUP §2
- [x] 🧑 Switch to Android, Player settings (package name, orientation: Auto Rotation, Landscape L/R only) — SETUP §3
- [x] 🧑 Import Starter Assets; Playground runs in the Editor — SETUP §4 (🤝 if import errors need fixing)
- [x] 🧑 Build And Run on the phone (Third Person Playground): character moves with the on-screen joystick — SETUP §3–4
- [x] 🤝 Create the public GitHub repo + first push (agent can use `gh` if you're logged in) — SETUP §5
- [x] 🤖 Scaffold the Colyseus server in `server/` and confirm `http://localhost:2567` — SETUP §6
- [x] 🧑 Windows Firewall rule for TCP 2567 (admin PowerShell) — SETUP §6
- [x] 🤖 `NetTest` script (`client/Assets/_Project/Scripts/Net/NetTest.cs`, retagged from 🤝) joins the default room from the Editor, then from the phone — SETUP §6
- [x] 🤖 Fill in `docs/testing/QA-PROFILE.md` (retagged from 🧑)
- [ ] 🧑 Submit GDD + paper-prototype photo (Checkpoint 1). No instructor questions: we follow the handout and document deviations (by: me).

### Phase 0b — CI/CD pipeline (Lab 6) · tooling per [Server Test and Lint Tooling](./decisions/14-server-test-tooling.md)
- [x] 🧑 SSH already works (remote `yaji.github.com:YajiHub/still-water.git`); take the CP1 screenshot (`node -v`, `npm -v`, `git --version`, `ssh -T`) — SETUP §7.1 (📸 CP1)
- [x] 🤖 `GET /` status page + `GET /health` (`{status, version}` from `APP_VERSION`) — SETUP §7.2. Playground moved to `/playground` (dev only).
- [x] 🤝 First pure logic function `isMovePlausible()` in `server/src/logic/movement.ts`: agent wrote signature + 9 tests, you wrote the body (13/13 pass)
- [x] 🤖 ESLint (typescript-eslint), `typecheck`, `test` = `node --import tsx --test --test-concurrency=1` (port `MyRoom.test.ts`, drop mocha); lockfile committed (📸 CP2 once the body is in)
- [x] 🤖 `server/Dockerfile`, `server/compose.yaml` (`8080:2567`), `.dockerignore`
- [x] 🤖 `.github/workflows/ci-cd.yml` `test` job (SHA-pinned actions) → green on GitHub (`d96878c`) (📸 CP3–CP4)
- [x] 🧑 Ubuntu Server VM + Docker + manual dry run (VMware, Ubuntu 26.04.1, `192.168.95.132`) + Windows firewall 8080 (phone reached `:8080`) — SETUP §7.4 (📸 CP5)
- [x] 🧑 Register the self-hosted runner as a service (label `deploy`, runner `stillwater-vm`, Idle) — SETUP §7.5
- [x] 🤖 `deploy` job with the health-check loop → green on GitHub (`1f9d0bf`, runner `stillwater-vm`), `/health` shows the SHA (📸 CP6)
- [x] 🧑 Ruleset `protect-main`; Tests A/B/C with timings — SETUP §7.6 (🤝 Test B: you choose what to break)

### Phase 1 — Feel prototype · per [Player Controller Architecture](./decisions/15-player-controller-architecture.md), [Grey-box Level Build Method](./decisions/16-grey-box-level-build-method.md), [Control Scheme](./decisions/02-control-scheme.md) touch layout
Branch `feat/phase1-feel`. Unity checks: EditMode tests in the Test Runner + play on the phone. Server checks: `npm run lint && npm run typecheck && npm test`.
- [x] 🤖 Assemblies: `_Project/Scripts/StillWater.asmdef` (+ `Editor/` asmdef) and `_Project/Tests/EditMode/StillWater.Tests.EditMode.asmdef`; one placeholder test green in the Test Runner; `NetTest` still compiles
- [x] 🤝 `JumpRules` (pure C#): coyote time, jump buffer, variable jump height. Agent wrote signatures + 15 EditMode tests (RED 15/16); bodies by human (autocomplete) + agent fix of `CanJump`; 16/16 green
- [x] 🤖 `MovementConfig` ScriptableObject (DESIGN §3.2–3.3 values + Draft extras: `lowJumpMultiplier` 2.5, `swimDepth`, `climbReach`, look/camera numbers); the asset is created by *Build Level* (Editor-verified)
- [x] 🤖 Server: `server/src/levels/level01.json` + `server/src/logic/layout.ts` (parse + validate: unique ids, sizes > 0, every gap ≤ max jump) + `node:test`; CI stays green (23/23 local, lint + typecheck 0)
- [x] 🤖 C# `LevelLayout` + Editor menu *Still Water → Build Level* (pool, water plane, platforms with edge-glow strips, start, tall exit edge + door; copies the JSON to `Configs/Levels/`); 🧑 run it and save `Scenes/Levels/Level01.unity`
- [x] 🤖 `PlayerMotor`: walk, jump via `JumpRules`, air control, gravity, deep-water swim at the surface, Climb at a safe edge (not the exit: raised `exitTopY`, button shows TOO HIGH), blob-shadow landing marker. Editor-verified
- [x] 🤖 Touch UI on `StarterAssetsInputs` (laptop + phone verified; `FrameRate` caps at 60, stable on the phone): floating move stick (left), drag-to-look (right half), Jump/Climb button; no look stick, no Sprint. IMGUI for the prototype (`shortcut:` → UI Toolkit in Phase 5); pause button moved to Phase 2 with the pause menu
- [x] 🤖 Camera rigs: `FirstPersonLook` (FOV ~80°, mid-air down-tilt, landing dip) and a third-person orbit rig (own script, not Cinemachine, so both cameras read input the same way); an on-screen debug toggle to switch them on the phone
- [x] 🧑 New scene → *Still Water → Build Level* → *Still Water → Create Player Rig* → save `Scenes/Levels/Level01.unity` → Build And Run on the phone (the rig menu replaces manual prefab wiring)
- [ ] 🧑 (deferred) Tune `MovementConfig` on the phone until the 10-jump bar passes (so far: `lowJumpMultiplier` 2.5 → 1.5 fixed the "ceiling bump" on quick taps)
- [x] 🧑 Camera comparison (5 min each) → lock [Camera Style](./decisions/05-camera-style.md): **first person** won (immersive; misses felt like the player's fault)
- [x] 🤝 Art spike (≤1 day): URP Volume profiles for options A and C on the grey box, phone screenshots + fps → lock [Art Style](./decisions/08-art-style.md). Agent built: ceiling + 4 hall lights in *Build Level*, *Still Water → Art Spike → Set Up Looks* (`LookProfile` A/C + Volume profiles in `Art/Looks/`), `LookSwitch` + on-phone **Look** toggle; human: compared on the phone (55–60 fps both), screenshots in `docs/screenshots/` → **C. Hybrid** locked, brightened
- [ ] 🧑 (deferred) Exit bar: 1 outside tester clears L1 in ≤3 tries unaided; 30+ fps; note results in the session log

### Phase 2 — Core loop + progression · per [Core Loop Rules](./decisions/04-core-loop-rules.md), [Save Data](./decisions/11-save-data.md)
Branch `feat/phase2-core-loop` (from `main` after the Phase 1 PR merges). Same checks as Phase 1. Screens are grey-box IMGUI like `TouchControls` (`shortcut:` → real UI in Phase 5).
- [x] 🤖 `safePath` in `level01.json` (~7 ids, start → exit) + validation on both sides (ids exist, every step ≤ max gap, last → exit) + tests; *Build Level* marks non-path platforms as fake candidates. Done: Level 1 regenerated with a 6-tile zig-zag path (`2 → 6 → 11 → 15 → 20 → 24`) + 18 dead ends (server test: every path tile is required at a 3.4 m reach), `MAX_GAP_M` 3.2 → 2.8, server 36/36
- [x] 🤝 `ProgressRules` (pure C#): `IsUnlocked`, `MergeBest` (max stars, min time), `Stars(splashes, time, par)`. Agent wrote signatures + 15 tests (RED 15 fail); `IsUnlocked` logic by human, `IndexOf` fixed + other bodies by agent at the human's request; 33/33
- [x] 🤖 `SaveSystem`: `save.json` atomic write (temp → replace), defaults on missing/corrupt/unknown version; 7 EditMode tests on a temp folder (RED 7 → 40/40)
- [x] 🤖 `LevelConfig` ScriptableObject per level: layout TextAsset, `fakePlatforms` switch (**off for Level 1**), goal text, glow time per tile, timer, par time, Listener alert delay + hunt speed
- [x] 🤖 Fake platforms: sink 0.4 s after landing (jump off in time = saved), stay sunk, reset on retry. Only active when `fakePlatforms` is on; verified in the Editor by switching it on for a test run (first real use: Level 2, Phase 3)
- [x] 🤖 `PatternRevealer`: overhead camera (~8 m), path tiles glow in order with a rising chime, glide back into the eyes, unlock controls; callable again (for [Pattern Re-show](./decisions/17-pattern-reshow.md)). Level 1 skips it (`revealPath` off); first real use Level 2. A fly-along camera was tried and dropped (messy)
- [x] 🤖 `LevelController`: Intro → Reveal → Crossing ⇄ in water → Won | Lost, Paused; timer from Reveal end; splash count; save before Result
- [x] 🤖 `ListenerAI` (grey-box shape): Dormant → Alerted (delay) → Hunting (+10%/extra splash) → Catch | Returning; safe once CLIMB starts; 3D splash sound + heartbeat (free CC0 audio → [CREDITS](../../docs/CREDITS.md))
- [x] 🤖 Screens + flow: Main Menu → Level Select (locks, stars) → Level → HUD (timer, splashes, pause) → Result (stars, time, Retry / Next / Menu); `GameState`; Reset progress (debug). Editor-verified: win → 3 stars → `save.json` written (17.95 s); placeholder sounds generated in code (`ProceduralAudio`, `shortcut:`)
- [ ] 🧑 Phone playtest of the full Level 1 loop: win, lose by catch, lose by timer, stars, force-close → progress kept (lab R3–R5 evidence)
- [ ] 🧑 Decide [Pattern Re-show](./decisions/17-pattern-reshow.md): moved out of Phase 2 (blocked until a level uses the Reveal)
- [ ] 🧑 Phase 1 leftovers: 10-jump bar + 1 outside tester (now with the real loop)

### Phase 3 — Levels 2–5+ · per [Level Roster](./decisions/18-level-roster.md) (one level at a time)
Branch `feat/phase3-levels`. Each level: plan with the user → generate layout → build scene → playtest → tune.
- [x] 🤖 Level generator `server/scripts/generate-level.ts` (`npm run level:gen`): seeded, hall size, route length, gap range, platform sizes; dead-end rule via the tested `reachesExit`. Level JSON gains `"oneDryRoute": true`, and the dead-end test runs for **every** level with it (not just level01). Done: 7 generator tests (RED: module missing + level01 flag), server 44/44
- [x] 🤖 `level02.json` (40 × 50 m, ~40 platforms incl. some 1.6 m, ~10-jump route, gaps 2.0–2.6 m) + server tests green. Done: seed 5 → 40 platforms (14 × 1.6 m), route of 10 tiles = 11 jumps, gaps 2.05–2.55 m; server 55/55 (+10 dead-end checks)
- [x] 🤖 Editor menu *Still Water → Update Build List* (Menu + every level scene in name order, without recreating the Menu scene; *Create Menu Scene* reuses it). Manual check: in the 🧑 step below
- [x] 🤖 Roaming Listener (user request, DESIGN §5.2): always in the pool, never teleports; roams → hears → swims to the sound / tracks you while swimming → lingers → roams on; `ListenerRules` (pure, 17 EditMode tests) + per-level roam mode Fin / Wake / Hidden + glimpses in `LevelConfig`. Done: RED 10/57 → 57/57 EditMode
- [x] 🤖 Dark-look readability (user request): lamp grid every ~12 m in *Build Level*, runtime headlamp + Listener glow from `LookProfile` (Look_C values via *Set Up Looks*); 🧑 Mobile renderer → Forward+, check fps on the phone. Done: Forward+, L1 6 / L2 12 lamps, phone **55–60 fps** in Look C
- [x] 🧑 Level02 scene: New Scene (Basic URP) → save `Scenes/Levels/Level02` → *Build Level* (`level02.json`) → *Create Player Rig* → *Set Up Looks* → *Set Up Level Loop* → `LevelConfig_02` values from the roster (name, goal, Listener 3.8 / 1.2 s, timer 150 / par 70, Reveal + fakes off) → *Update Build List*
- [x] 🧑 Play L1 → clear → **Next** → L2 (unlock flow), then playtest L2 on the phone; tune. Playtest 1: L2 not harder than L1 → 🤖 regenerated (50 platforms, 40% small, gaps 2.2–2.7, server 54/54); 🧑 Build Level (timer 110 / par 55 already set; Listener user-tuned 11 m/s · 0 s), then playtest 2. Playtest 2 (phone): ★★★ in 43 s (par 55), careful run → par kept. Outside tester still to come (with the Phase 1 one)
- [ ] 🧑 Plan Level 3 with the agent (wayfinder, Level Roster)

## Destination
A submitted lab build: an Android APK of a 3D poolrooms horror platformer with 5 unlockable levels, offline solo (+ AI rival if time allows), and an online 1v1 race through a deployed Colyseus server. Meets R1–R10, with playtest-driven fixes, GDD, README and paper. The same repo satisfies Lab 6: the server is linted, tested and auto-deployed by GitHub Actions to a VM through a self-hosted runner, with Tests A/B/C documented and its own paper. The codebase is clean enough to keep growing as a portfolio piece afterwards.

## Notes & Invariants
- Design truth: [docs/DESIGN.md](../../docs/DESIGN.md). Tech truth: [docs/SYSTEM_ATLAS.md](../../docs/SYSTEM_ATLAS.md). Conventions & workflow: [docs/CONVENTIONS.md](../../docs/CONVENTIONS.md). Grading checklists: [Lab 5](../../docs/LAB5_REQUIREMENTS.md), [Lab 6](../../docs/LAB6_REQUIREMENTS.md).
- Server rules are pure, tested functions in `server/src/logic/`; the `test` job must stay green. Once the ruleset is on, changes reach `main` only through PRs.
- Budget ₱0: no paid services, assets or plugins.
- Offline mode never touches the network. Online always fails gracefully to Mode Select (R6, R8).
- Online: the server decides splashes, catches and the winner. Clients only move themselves.
- All gameplay numbers live in ScriptableObjects / config, never hard-coded.
- Progress is saved before the Result screen is shown.
- Never guess Unity 6 / Cinemachine / Input System / Colyseus APIs. Check the installed versions' docs.
- Original or properly licensed assets only, credited in the same commit.
- One fixed orientation for the whole game (R9).

## Decisions So Far
- [Engine and Stack](./decisions/01-engine-and-stack.md) (by: me): Unity 6 (C#, URP) client + Colyseus (Node/TypeScript) server in Docker (deployed by CI/CD to a VM; optional Render); Supabase optional for the leaderboard.
- [Lab 6 Integration](./decisions/13-lab6-integration.md) (by: me): one public repo; the Lab 6 pipeline lints/tests/deploys `server/` to an Ubuntu VM via a self-hosted runner; tags `lab5-v1.0` / `lab6-v1.0`; no workflow-level path filter.
- [Control Scheme](./decisions/02-control-scheme.md) (by: me): floating virtual joystick + Jump button, free 3D movement with feel helpers (not tap-to-hop); Starter Assets touch input.
- [Orientation](./decisions/03-orientation.md) (by: me): landscape, auto-rotate Left/Right only; UI reference 1920×1080, safe area respected.
- [Hosting and LAN Plan](./decisions/12-hosting-and-lan.md) (by: delegated): one Colyseus server; Option A = VM on the laptop (`ws://<Wi-Fi IP>:8080`), Option B = same image on Render (`wss`), dev on :2567; in-app server picker. ⚠️ instructor OK pending.
- [Server Test and Lint Tooling](./decisions/14-server-test-tooling.md) (by: delegated): `node:test` + `tsx` (mocha crashes on Windows), ESLint + typescript-eslint, `tsc --noEmit`; CI: `npm ci` → lint → typecheck → test.
- [Player Controller Architecture](./decisions/15-player-controller-architecture.md) (by: me): own `PlayerMotor` + pure `JumpRules` (EditMode-tested) + `MovementConfig` SO; swappable first/third-person camera rigs; Starter Assets for input and art only.
- [Grey-box Level Build Method](./decisions/16-grey-box-level-build-method.md) (by: me): layouts as JSON in `server/src/levels/`; Editor menu *Still Water → Build Level* builds the grey box and copies the JSON into the client; server `node:test` validates every layout.
- [Camera Style](./decisions/05-camera-style.md) (by: me): first person + jump aids, from the Phase 1 phone test; third person stays as a debug toggle until Phase 5.
- [Art Style](./decisions/08-art-style.md) (by: me): C. Hybrid horror (fog, cold dim lights, grain), from the Phase 1 phone spike; values in `Art/Looks/Look_C`.
- [Core Loop Rules](./decisions/04-core-loop-rules.md) (by: me): only the safe path (`safePath` ids in the JSON) holds you, all others fake **from Level 2** (Level 1 has no fakes); overhead-camera Reveal; timer after Reveal, 0 = lose; fixed path offline; levels grow bigger/sparser, max gap 2.8 m.
- [Save Data](./decisions/11-save-data.md) (by: me): `save.json` in persistentDataPath, per-level records by name (`level01`), open = previous cleared, best-only merge, atomic write before Result, corrupt → defaults.

## Active Frontier (Open & Unblocked)
- [ ] [Level Roster](./decisions/18-level-roster.md) — grill, per level (Level 2 locked; Level 3 next, from the user's idea)
- [ ] [Online Match Format](./decisions/07-online-match-format.md) — grill
- [ ] [Offline Rival](./decisions/09-offline-rival.md) — grill
- [ ] [Title and Monster Identity](./decisions/06-title-and-monster.md) — grill

## Blocked
- [ ] [Pattern Re-show](./decisions/17-pattern-reshow.md) — grill (Blocked by: [Level Roster](./decisions/18-level-roster.md): only matters once a level uses the Reveal)
- [ ] [Netcode Model](./decisions/10-netcode-model.md) — domain-model (Blocked by: [Online Match Format](./decisions/07-online-match-format.md))

## Fog of War (Not Yet Specified)
- Ledge grab and visible first-person shoes/legs (first person won Camera Style; Phase 2 or 5)
- Exact layouts and tuning for Levels 2–5 (needs the Level 1 feel prototype first)
- Tutorial / first-time hints design for Level 1
- Audio details: which sounds, sources, mixing
- Accessibility options (text size, reduced flicker, glow color)
- Online leaderboard scope (Supabase) — only if Phase 4 finishes early
- Team composition and Git workflow (solo vs team; branch naming)
- Playtest logistics (who the 5+ testers are, which 2 phones)
- Paper outlines and reference gathering (two papers: Lab 5 game/playtest, Lab 6 pipeline)

## Out of Scope (for the lab submission)
- iOS build · Play Store release (post-lab, needs the $25 fee) · accounts / login · more than 2 players online · voice chat · procedural levels · ads / monetization · console/PC ports · Unity client CI builds (GameCI) — post-lab idea
