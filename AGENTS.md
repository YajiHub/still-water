# AGENTS.md — Still Water (ITSD 85 Labs 5 & 6)

**What:** a 3D Android horror platformer (Unity 6.3 LTS `6000.3.27f2`, C#, URP). Memorize the glowing safe platforms,
jump across a flooded "poolrooms" hall, and don't splash, because the Listener hunts by sound. 5+ unlockable levels,
offline solo, and an online 1v1 race on a **Colyseus (Node 24 + TypeScript)** server. **Lab 6:** the same server is
linted, tested and auto-deployed by GitHub Actions to an Ubuntu VM through a self-hosted runner. Budget ₱0.

## Current Status & Next Step
- **Phase:** 0 (setup) 🟡. No code yet. Active plan: [planning/still-water/MAP.md](planning/still-water/MAP.md)
- **Next:** `execute-phase Phase 0` ([SETUP.md](docs/SETUP.md) §2 onward has the step details)
- **Latest session:** [docs/sessions/2026-11-02-setup.md](docs/sessions/2026-11-02-setup.md)

## Routing (one home per fact)
| Need | File |
|---|---|
| Rules, dev environment (Windows + WSL/pi), workflow, ownership tags | [docs/CONVENTIONS.md](docs/CONVENTIONS.md) ← **read once per session** |
| Game design (source of truth) · lab one-pager | [docs/DESIGN.md](docs/DESIGN.md) · [docs/GDD.md](docs/GDD.md) |
| Architecture, folders, protocol, save format, CI/CD | [docs/SYSTEM_ATLAS.md](docs/SYSTEM_ATLAS.md) |
| Plan, phases, decisions | [planning/still-water/MAP.md](planning/still-water/MAP.md) + `decisions/` |
| Grading checklists | [Lab 5](docs/LAB5_REQUIREMENTS.md) · [Lab 6](docs/LAB6_REQUIREMENTS.md) |
| Machine setup steps | [docs/SETUP.md](docs/SETUP.md) |
| Session history · my knowledge gaps · QA profile | [docs/sessions/](docs/sessions/) · `docs/learning/gaps.md` (created by wrap-up) · `docs/testing/QA-PROFILE.md` (to add) |
| Ideas · asset credits | [docs/IDEAS.md](docs/IDEAS.md) · [docs/CREDITS.md](docs/CREDITS.md) |

Conflicts: decision files win → DESIGN.md / SYSTEM_ATLAS.md → GDD.md. Fix the loser.

## Commands
- **Server** (`server/`, once it exists): `npm run lint` · `npm run typecheck` · `npm test` · `npm run dev` (:2567)
- **Unity:** no CLI tests yet. Verify in the Editor (Play) and on the phone (Build And Run).

## Never
- Create, edit, move or delete `.meta` files, or hand-edit `.unity`/`.prefab`/`.asset` YAML. Give Editor steps instead.
- Implement an *Open* decision as if it were decided; guess Unity 6 / Cinemachine 3 / Input System / Colyseus APIs (check installed versions).
- Hard-code gameplay numbers (use ScriptableObjects/config), or let offline mode touch the network.
- Commit secrets (keystores, `.env`, runner tokens, SSH keys), or add unlicensed or uncredited assets.
- Commit or push unless asked. After the ruleset is on, changes reach `main` only via PRs.

Paths: Windows `C:\Users\LEGION2\Yaji\projects\still-water` · WSL `/mnt/c/Users/LEGION2/Yaji/projects/still-water`.
