# MAP — Still Water (Wayfinder, Map Mode)

> Index, not a store: one line per item, with details in `decisions/`. Overwrite in place; never append status history.

## Phase Progress

| Phase | Scope | Blocking Decisions | Status |
|---|---|---|---|
| 0. Decide & set up | ✅ Unity 6.3 LTS + Android modules installed. ✅ Repo initialized, Orientation locked, Unity project `client` created (Android active). Remaining: Player settings, Starter Assets on the phone, GitHub, Colyseus hello room reachable from the phone. Checklist: [SETUP.md](../../docs/SETUP.md). *(Lab checkpoints 1–2: GDD approved, repo runs)* | [Hosting and LAN Plan](./decisions/12-hosting-and-lan.md) (instructor question only) | 🟡 In Progress |
| 0b. CI/CD pipeline (Lab 6) | Server `/` + `/health`, first pure logic function + tests, ESLint/typecheck, Dockerfile + compose; Ubuntu Server VM + Docker + self-hosted runner; `ci-cd.yml` test → deploy; ruleset `protect-main`; Tests A/B/C. Checklist: [LAB6_REQUIREMENTS.md](../../docs/LAB6_REQUIREMENTS.md), [SETUP.md §7](../../docs/SETUP.md#7-cicd-pipeline-lab-6). *(Lab 6 checkpoints 1–6; Tests A/B/C can be repeated later with real features)* | [Server Test and Lint Tooling](./decisions/14-server-test-tooling.md) | [ ] Pending |
| 1. Feel prototype | Grey-box Level 1. Joystick + jump + feel helpers + swim/climb-out. Camera test. 1-day art spike. Must feel good before moving on. | [Camera Style](./decisions/05-camera-style.md), [Art Style](./decisions/08-art-style.md) | [ ] Pending |
| 2. Core loop + progression | Reveal, safe/fake platforms, splash → Listener → catch, win/lose, stars, GameState, save, Level Select with locks, Result screen | [Core Loop Rules](./decisions/04-core-loop-rules.md), [Save Data](./decisions/11-save-data.md) | [ ] Pending |
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
- [ ] 🧑 Switch to Android, Player settings (package name, orientation: Auto Rotation, Landscape L/R only) — SETUP §3
- [x] 🧑 Import Starter Assets; Playground runs in the Editor — SETUP §4 (🤝 if import errors need fixing)
- [ ] 🧑 Build And Run on the phone: character moves with the on-screen joystick — SETUP §3–4
- [ ] 🤝 Create the public GitHub repo + first push (agent can use `gh` if you're logged in) — SETUP §5
- [ ] 🤖 Scaffold the Colyseus server in `server/` and confirm `http://localhost:2567` — SETUP §6
- [ ] 🧑 Windows Firewall rule for TCP 2567 (admin PowerShell) — SETUP §6
- [ ] 🤝 `NetTest` script joins the default room from the Editor, then from the phone — SETUP §6
- [ ] 🧑 Copy the QA profile template to `docs/testing/QA-PROFILE.md` and fill it in
- [ ] 🧑 Send the instructor the Lab 6 integration + Option A questions; submit GDD + paper-prototype photo

### Phase 0b — CI/CD pipeline (Lab 6) · blocked by [Server Test and Lint Tooling](./decisions/14-server-test-tooling.md)
- [ ] 🧑 SSH key on GitHub, `ssh -T`, switch the remote to SSH — SETUP §7.1 (📸 CP1)
- [ ] 🤖 `GET /` status page + `GET /health` (`{status, version}` from `APP_VERSION`) — SETUP §7.2
- [ ] 🤝 First pure logic function `isMovePlausible()` in `server/src/logic/`: agent writes the signature + tests, you write the body
- [ ] 🤖 ESLint (typescript-eslint), `typecheck`, `test` scripts; lockfile committed (📸 CP2)
- [ ] 🤖 `server/Dockerfile`, `server/compose.yaml` (`8080:2567`), `.dockerignore`
- [ ] 🤖 `.github/workflows/ci-cd.yml` `test` job → green (📸 CP3–CP4)
- [ ] 🧑 Ubuntu Server VM + Docker + manual dry run + firewall 8080 — SETUP §7.4 (📸 CP5)
- [ ] 🧑 Register the self-hosted runner as a service (label `deploy`) — SETUP §7.5
- [ ] 🤖 `deploy` job with the health-check loop → green, SHA visible (📸 CP6)
- [ ] 🧑 Ruleset `protect-main`; Tests A/B/C with timings — SETUP §7.6 (🤝 Test B: you choose what to break)

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
- [Control Scheme](./decisions/02-control-scheme.md) (by: me): floating virtual joystick + Jump button, free 3D movement with feel helpers (not tap-to-hop), built on Unity Starter Assets – Third Person.
- [Orientation](./decisions/03-orientation.md) (by: me): landscape, auto-rotate Left/Right only; UI reference 1920×1080, safe area respected.

## Active Frontier (Open & Unblocked)
- [ ] [Hosting and LAN Plan](./decisions/12-hosting-and-lan.md) — research (+ ask the instructor about Option A)
- [ ] [Server Test and Lint Tooling](./decisions/14-server-test-tooling.md) — research
- [ ] [Camera Style](./decisions/05-camera-style.md) — prototype
- [ ] [Art Style](./decisions/08-art-style.md) — prototype
- [ ] [Core Loop Rules](./decisions/04-core-loop-rules.md) — domain-model
- [ ] [Save Data](./decisions/11-save-data.md) — domain-model
- [ ] [Online Match Format](./decisions/07-online-match-format.md) — grill
- [ ] [Offline Rival](./decisions/09-offline-rival.md) — grill
- [ ] [Title and Monster Identity](./decisions/06-title-and-monster.md) — grill

## Blocked
- [ ] [Netcode Model](./decisions/10-netcode-model.md) — domain-model (Blocked by: [Online Match Format](./decisions/07-online-match-format.md))

## Fog of War (Not Yet Specified)
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
