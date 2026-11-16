# AGENTS.md — Still Water (ITSD 85 Labs 5 & 6)

**What:** a 3D Android horror platformer (Unity 6.3 LTS `6000.3.27f2`, C#, URP). Memorize the glowing safe platforms,
jump across a flooded "poolrooms" hall, and don't splash, because the Listener hunts by sound. 5+ unlockable levels,
offline solo, and an online 1v1 race on a **Colyseus (Node 24 + TypeScript)** server. **Lab 6:** the same server is
linted, tested and auto-deployed by GitHub Actions to an Ubuntu VM through a self-hosted runner. Budget ₱0.

## Current Status & Next Step
- **Phase:** 4 (Online LAN race) 🟡 on `feat/lan-race`: **all 5 offline levels exist** (L5 The Drain on `e45ff29`), plus lab-ready LAN 1v1 server (`RaceRoom` + `race.ts`, 156/156 server tests) and Unity client (`LanRace`, `OnlineLevelController`, `MainMenu` Online 1v1 + Sound toggle + Credits) implemented; awaiting Unity compile, EditMode Test Runner, and 2-device LAN + APK verification. Active plan: [planning/lab-ready-plan.md](planning/lab-ready-plan.md) · roadmap: [planning/still-water/MAP.md](planning/still-water/MAP.md)
- **Next:** Unity compile + EditMode Test Runner → Editor + phone LAN smoke test (`npm start` on Windows :2567) → two-device LAN race + airplane-mode offline + 5-level save checks → Android APK build (`Builds/Still Water.apk`). Visual menus: *Still Water → Art Spike → Build Listener Prefab / Set Up Water / Set Up Hall*; hand-edited levels: *Export Level to JSON* → `npm run level:route` → *Build Level*.
- **Latest session:** [docs/sessions/2026-11-07-level5-drain.md](docs/sessions/2026-11-07-level5-drain.md)

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
- **Server** (`server/`, run on Windows Node 24): `npm start` (tsx watch, :2567) · `npm run lint` · `npm run typecheck` · `npm test` (`node:test`) · `npm run build` · `npm run level:gen -- --id levelNN …` (seeded layout generator; options in `scripts/generate-level.ts`) · `npm run level:route -- --id levelNN [--max-gap 3.1] [--hand-tuned]` (after *Export Level to JSON* from a hand-edited scene). Docker: `docker compose -p stillwater-local up -d --build` (:8080).
- **Unity:** EditMode tests in Window → General → Test Runner (results: `%USERPROFILE%\AppData\LocalLow\YajiHub\Still Water\TestResults.xml`); Play in the Editor; phone via Build And Run.

## Never
- Create, edit, move or delete `.meta` files, or hand-edit `.unity`/`.prefab`/`.asset` YAML. Give Editor steps instead.
- Implement an *Open* decision as if it were decided; guess Unity 6 / Cinemachine 3 / Input System / Colyseus APIs (check installed versions).
- Hard-code gameplay numbers (use ScriptableObjects/config), or let offline mode touch the network.
- Commit secrets (keystores, `.env`, runner tokens, SSH keys), or add unlicensed or uncredited assets.
- Commit or push unless asked. After the ruleset is on, changes reach `main` only via PRs.

Paths: Windows `C:\Users\LEGION2\Yaji\projects\still-water` · WSL `/mnt/c/Users/LEGION2/Yaji/projects/still-water`.
