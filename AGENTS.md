# AGENTS.md — Still Water (ITSD 85 Labs 5 & 6)

**What:** a 3D Android horror platformer (Unity 6.3 LTS `6000.3.27f2`, C#, URP). Memorize the glowing safe platforms,
jump across a flooded "poolrooms" hall, and don't splash, because the Listener hunts by sound. 5+ unlockable levels,
offline solo, and an online 1v1 race on a **Colyseus (Node 24 + TypeScript)** server. **Lab 6:** the same server is
linted, tested and auto-deployed by GitHub Actions to an Ubuntu VM through a self-hosted runner. Budget ₱0.

## Current Status & Next Step
- **Phase:** 2 (Core loop) 🟡: merged to `main` (PR #13, deployed `532b6d9`), Editor-verified; left: phone playtest. Phase 3 (Levels) next: Level 2 locked in [Level Roster](planning/still-water/decisions/18-level-roster.md). Deferred: Phase 1 10-jump bar + outside tester; Phase 0 Checkpoint 1 (GDD + paper-prototype photo). Active plan: [planning/still-water/MAP.md](planning/still-water/MAP.md)
- **Next:** `git switch -c feat/phase3-levels` → `/skill:execute-phase Phase 3`. Unity verification: Test Runner (EditMode, 40) + `Editor.log`; menus *Still Water → Build Level / Create Player Rig / Set Up Level Loop*.
- **Latest session:** [docs/sessions/2026-11-05-phase2-core-loop.md](docs/sessions/2026-11-05-phase2-core-loop.md)

## Routing (one home per fact)
| Need | File |
|---|---|
| Rules, dev environment (Windows + WSL/pi), workflow, ownership tags | [docs/CONVENTIONS.md](docs/CONVENTIONS.md) ← **read once per session** |
| Game design (source of truth) · lab one-pager | [docs/DESIGN.md](docs/DESIGN.md) · [docs/GDD.md](docs/GDD.md) |
| Architecture, folders, protocol, save format, CI/CD | [docs/SYSTEM_ATLAS.md](docs/SYSTEM_ATLAS.md) |
| Plan, phases, decisions | [planning/still-water/MAP.md](planning/still-water/MAP.md) + `decisions/` |
| Grading checklists | [Lab 5](docs/LAB5_REQUIREMENTS.md) · [Lab 6](docs/LAB6_REQUIREMENTS.md) |
| Machine setup steps | [docs/SETUP.md](docs/SETUP.md) |
| Session history · my knowledge gaps · QA profile | [docs/sessions/](docs/sessions/) · [docs/learning/gaps.md](docs/learning/gaps.md) · [docs/testing/QA-PROFILE.md](docs/testing/QA-PROFILE.md) |
| Ideas · asset credits | [docs/IDEAS.md](docs/IDEAS.md) · [docs/CREDITS.md](docs/CREDITS.md) |

Conflicts: decision files win → DESIGN.md / SYSTEM_ATLAS.md → GDD.md. Fix the loser.

## Commands
- **Server** (`server/`, run on Windows Node 24): `npm start` (tsx watch, :2567) · `npm run lint` · `npm run typecheck` · `npm test` (`node:test`) · `npm run build`. Docker: `docker compose -p stillwater-local up -d --build` (:8080).
- **Unity:** EditMode tests in Window → General → Test Runner (results: `%USERPROFILE%\AppData\LocalLow\YajiHub\Still Water\TestResults.xml`); Play in the Editor; phone via Build And Run.

## Never
- Create, edit, move or delete `.meta` files, or hand-edit `.unity`/`.prefab`/`.asset` YAML. Give Editor steps instead.
- Implement an *Open* decision as if it were decided; guess Unity 6 / Cinemachine 3 / Input System / Colyseus APIs (check installed versions).
- Hard-code gameplay numbers (use ScriptableObjects/config), or let offline mode touch the network.
- Commit secrets (keystores, `.env`, runner tokens, SSH keys), or add unlicensed or uncredited assets.
- Commit or push unless asked. After the ruleset is on, changes reach `main` only via PRs.

Paths: Windows `C:\Users\LEGION2\Yaji\projects\still-water` · WSL `/mnt/c/Users/LEGION2/Yaji/projects/still-water`.
