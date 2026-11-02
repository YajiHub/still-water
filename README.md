# Still Water *(working title)* — ITSD 85 Labs 5 & 6

> Placeholder README in the format both lab handouts require. Fill it in near submission.
> For project context (humans and AI assistants), start at [AGENTS.md](AGENTS.md).

Student(s): _TBD_  Section: _TBD_  Instructor: _TBD_  Platform: Android

This repository holds two labs:
- **Lab 5:** a mobile game with level progression and offline/online multiplayer (`client/` Unity game + `server/` Colyseus server). Release: `lab5-v1.0`.
- **Lab 6:** automated CI/CD. The game server is linted, tested and deployed automatically by GitHub Actions to a self-hosted Ubuntu server. Release: `lab6-v1.0`.

---

# Lab 5: Mobile Game

## About the game
_One-paragraph pitch + 2 screenshots._

## How to play
_Controls, goal, how levels unlock._

## Offline and online modes
_How to play offline; how to join an online match (server picker: Local dev / LAN VM / Cloud)._

## How to build
_Unity 6000.3.27f2 + Android Build Support; export steps. Server: `cd server && npm install && npm start`._

## Download
_Link to the `lab5-v1.0` release APK._

## Playtest results
_Top 3 findings and fixes._

## Credits
See [docs/CREDITS.md](docs/CREDITS.md).

## Paper
See `docs/paper-lab5.pdf` (added at submission).

---

# Lab 6: Automated CI/CD Pipeline

## What this project does
_The game server is tested on every push/PR and auto-deployed to a self-hosted server when tests pass on `main`._

## Pipeline diagram
_Image in `docs/pipeline.png`._

## How to run locally
_`cd server && npm ci && npm run lint && npm run typecheck && npm test && npm run dev`_

## How the deployment works
_test job (GitHub cloud runner) → deploy job (self-hosted runner in the Ubuntu VM) → Docker Compose → health check on `/health` showing the commit SHA._

## Results
_Push-to-live times from Tests A–C (see [docs/LAB6_REQUIREMENTS.md](docs/LAB6_REQUIREMENTS.md))._

## Screenshots
_Links to `docs/screenshots/` (cp1-versions.png, …)._

## Paper
See `docs/paper-lab6.pdf` (added at submission).
