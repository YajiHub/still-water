# AGENTS.md — Still Water (ITSD 85 Labs 5 & 6)

**What:** a 3D Android horror platformer (Unity 6.3 LTS `6000.3.27f2`, C#, URP). Memorize the glowing safe platforms,
jump across a flooded "poolrooms" hall, and don't splash, because the Listener hunts by sound. 5+ unlockable levels,
offline solo, and an online 2–4 player co-op race through all five rooms on a **Colyseus (Node 24 + TypeScript)**
server (hosted free on Render). **Lab 6:** the same server is
linted, tested and auto-deployed by GitHub Actions to an Ubuntu VM through a self-hosted runner. Budget ₱0.

## Current Status & Next Step
- **Phase:** 4–5 🟡, all on `main` (PRs #20, #21).
  - 5 offline levels.
  - The original Blender Listener (`art/listener/build_listener.py`).
  - Quaternius player bodies picked in Settings.
  - Original music (`art/music/compose.py`).
  - The online **2–4 player co-op race circuit**: server `circuit` room (175/175 tests) and client `CircuitWorld`.
  - A Settings page.
  - The server live on Render free tier (`https://still-water-server.onrender.com`, `render.yaml`).
  - Roadmap: [planning/still-water/MAP.md](planning/still-water/MAP.md)
- **Next:**
  1. Unity EditMode Test Runner (new: `CircuitSnapshotTests`, `PlayerLookTests`, `ListenerTuningParityTests`).
  2. *Still Water → Art Spike → Build Listener Prefab* (and *Build Player Bodies* / *Build Menu Backdrop* after
     changing those builders).
  3. Build And Run on phones, then a 2+ phone co-op run over Render: push strength, control size, catch camera, fps
     in rooms 3–5.
  4. APK build (`Builds/Still Water.apk`).
  - Regenerate art: `blender -b --python art/listener/build_listener.py -- …` (see `art/*/README.md`).
  - Hosting / hotspot: [docs/SETUP.md](docs/SETUP.md) §8–9.
- **Latest session:** [docs/sessions/2026-11-16-listener-coop-render.md](docs/sessions/2026-11-16-listener-coop-render.md)

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
