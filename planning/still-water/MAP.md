# MAP — Still Water (Wayfinder, Map Mode)

> Index, not a store: one line per item, with details in `decisions/`. Overwrite in place; never append status history.

## Phase Progress

| Phase | Scope | Blocking Decisions | Status |
|---|---|---|---|
| 0. Decide & set up | ✅ Unity + Android, repo on GitHub, Orientation locked, `client` project (Android, Player settings), Starter Assets touch controls on the phone (First Person Playground), Colyseus hello room joined from Editor + phone (LAN `ws://192.168.1.9:2567`). server lockfile on registry.npmjs.org, QA profile. Third Person Playground on the phone (Colyseus SDK embedded in `client/Packages/io.colyseus.sdk` to drop a broken upstream symlink). Remaining: GDD + paper-prototype photo (Checkpoint 1). Checklist: [SETUP.md](../../docs/SETUP.md). *(Lab checkpoints 1–2: GDD approved, repo runs)* | — | 🟡 In Progress |
| 0b. CI/CD pipeline (Lab 6) | ✅ Server `/` + `/health`, lint/typecheck/`node:test` (13 tests), Docker image, `ci-cd.yml` test → deploy on the self-hosted VM runner (VMware, Ubuntu 26.04.1), ruleset `protect-main`, Tests A/B/C, 5 deploy timings ([results](../../docs/LAB6_REQUIREMENTS.md#results-table-part-75-fill-in-during-testing)), phone reaches `:8080`. Screenshots in `docs/screenshots/` (CP2, CP5, Test B, C-M1, C-M2); CP1, CP3, CP4, CP6, Test A, C-bad still to take (any time, from the Actions history). README + paper: Phase 6. | — | ✅ Done |
| 1. Feel prototype | Grey-box Level 1 built from `level01.json`; own `PlayerMotor` (jump aids, deep-water swim, climb-out); touch UI (floating stick, drag-to-look, Jump/Climb); first vs third person rigs; 1-day art spike. **Exit bar:** on the phone, 10 jumps in a row with ≤1 miss; misses feel like the player's fault; 1 new tester clears L1 in ≤3 tries unaided; steady 30+ fps; Camera Style + Art Style locked. | ~~[Camera Style](./decisions/05-camera-style.md)~~ ✅ first person, ~~[Art Style](./decisions/08-art-style.md)~~ ✅ C. Hybrid | 🟡 In Progress: built + phone-verified; left: 10-jump bar, 1 outside tester (deferred, can run during Phase 2) |
| 2. Core loop + progression | Reveal, safe/fake platforms, splash → Listener → catch, win/lose, stars, GameState, save, Level Select with locks, Result screen | ~~[Core Loop Rules](./decisions/04-core-loop-rules.md)~~ ✅, ~~[Save Data](./decisions/11-save-data.md)~~ ✅ | [ ] Pending (ready: task list below) |
| 3. Levels 2–5 + offline | Moving / sinking platforms, fog and flicker, difficulty curve, optional Ghost rival; airplane-mode test. *(Checkpoints 3–4)* | [Offline Rival](./decisions/09-offline-rival.md) | [ ] Pending |
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
- [ ] 🧑 Ubuntu Server VM + Docker + manual dry run ✅ (VMware, Ubuntu 26.04.1, `192.168.95.132`) · firewall 8080 still open — SETUP §7.4 (📸 CP5)
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
- [ ] 🤖 `safePath` in `level01.json` (~7 ids, start → exit) + validation on both sides (ids exist, every step ≤ max gap, last → exit) + tests; *Build Level* marks non-path platforms as fake candidates
- [ ] 🤝 `ProgressRules` (pure C#): `IsUnlocked`, `MergeBest` (max stars, min time), `Stars(splashes, time, par)`. Agent writes signatures + EditMode tests; you write the bodies
- [ ] 🤖 `SaveSystem`: `save.json` atomic write (temp → replace), defaults on missing/corrupt/unknown version; EditMode test on a temp folder
- [ ] 🤖 `LevelConfig` ScriptableObject per level: layout TextAsset, `fakePlatforms` switch (**off for Level 1**), goal text, glow time per tile, timer, par time, Listener alert delay + hunt speed
- [ ] 🤖 Fake platforms: sink 0.4 s after landing (jump off in time = saved), stay sunk, reset on retry. Only active when `fakePlatforms` is on; verified in the Editor by switching it on for a test run (first real use: Level 2, Phase 3)
- [ ] 🤖 `PatternRevealer`: overhead camera (~8 m), path tiles glow in order with a rising chime, glide back into the eyes, unlock controls; callable again (for [Pattern Re-show](./decisions/17-pattern-reshow.md))
- [ ] 🤖 `LevelController`: Intro → Reveal → Crossing ⇄ in water → Won | Lost, Paused; timer from Reveal end; splash count; save before Result
- [ ] 🤖 `ListenerAI` (grey-box shape): Dormant → Alerted (delay) → Hunting (+10%/extra splash) → Catch | Returning; safe once CLIMB starts; 3D splash sound + heartbeat (free CC0 audio → [CREDITS](../../docs/CREDITS.md))
- [ ] 🤖 Screens + flow: Main Menu → Level Select (locks, stars) → Level → HUD (timer, splashes, pause) → Result (stars, time, Retry / Next / Menu); `GameState`; Reset progress (debug)
- [ ] 🧑 Phone playtest of the full Level 1 loop: win, lose by catch, lose by timer, stars, force-close → progress kept (lab R3–R5 evidence)
- [ ] 🧑 Decide [Pattern Re-show](./decisions/17-pattern-reshow.md) after the playtest (wayfinder)
- [ ] 🧑 Phase 1 leftovers: 10-jump bar + 1 outside tester (now with the real loop)

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
- [ ] [Pattern Re-show](./decisions/17-pattern-reshow.md) — grill (decide after the Phase 2 playtest; does not block building)
- [ ] [Online Match Format](./decisions/07-online-match-format.md) — grill
- [ ] [Offline Rival](./decisions/09-offline-rival.md) — grill
- [ ] [Title and Monster Identity](./decisions/06-title-and-monster.md) — grill

## Blocked
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
