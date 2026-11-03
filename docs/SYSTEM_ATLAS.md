# System Atlas — Still Water

> **Status:** planned architecture (no code yet). This is the **source of truth for technical structure**.
> When code is created, replace "planned" paths with real ones and keep this file as a map
> an AI can follow straight to the right file instead of searching the whole repo.

---

## 1. Big picture

```
┌──────────────────────────── Android phone ────────────────────────────┐
│ Unity 6 client (C#, URP)                                              │
│                                                                       │
│  UI scenes ──► GameState (singleton) ──► SaveSystem ──► JSON file     │
│     │              ▲                                (persistentData)  │
│     ▼              │                                                  │
│  Level scene: Player · Platforms · Listener · LevelController         │
│     │                                                                 │
│     └── Online only: NetClient (Colyseus Unity SDK) ──────────────┐   │
└───────────────────────────────────────────────────────────────────│───┘
                                                                    │ WebSocket (ws on LAN, wss on Render)
┌──────────────────────── Colyseus server (Node + TS) ──────────────▼───┐
│  RaceRoom: holds layout + seed, validates moves, runs the online       │
│  Listener, decides splashes / catches / finish / winner                │
│  Dev: `npm start` on Windows (:2567) · Deployed by CI/CD to an Ubuntu  │
│  VM via self-hosted runner (LAN :8080) · optional Render (internet)    │
└───────────────────────────────────────────────────────────────────┬───┘
                                                                    │ (optional, bonus)
                                                    Supabase Postgres: leaderboard
```

**Key rule:** offline play uses only the left half of this diagram. The network side is never touched offline.

---

## 2. Repository layout (planned)

```
/
├── AGENTS.md · CLAUDE.md · README.md · .gitignore · .gitattributes
├── .github/workflows/ci-cd.yml             Lab 6 pipeline: test (cloud) → deploy (self-hosted VM)
├── client/                                 Unity project root (open this folder in Unity Hub)
│   ├── Assets/
│   │   ├── _Project/                       ← all our own content lives here
│   │   │   ├── Scenes/
│   │   │   │   ├── UI/  Title, MainMenu, ModeSelect, LevelSelect, Result, Lobby, OnlineResult, Settings, Credits
│   │   │   │   └── Levels/  Level01 … Level05, OnlineArena (if separate)
│   │   │   ├── Scripts/
│   │   │   │   ├── Core/       GameState, SaveSystem, SceneLoader, AudioManager, Settings
│   │   │   │   ├── Player/     PlayerMovement (from Starter Assets), FeelHelpers, Swim, BlobShadow
│   │   │   │   ├── Level/      LevelController, LevelConfig (ScriptableObject), Platform, PatternRevealer, ExitDoor
│   │   │   │   ├── Monster/    ListenerAI, ListenerConfig (SO)
│   │   │   │   ├── Offline/    GhostRival (if built)
│   │   │   │   ├── Net/        NetClient, NetSchema (generated), RemotePlayerView, OnlineLevelController
│   │   │   │   └── UI/         per-screen controllers, TouchControls, PauseMenu
│   │   │   ├── Configs/        LevelConfig_01 … 05.asset, MovementConfig.asset, ListenerConfig.asset
│   │   │   ├── Prefabs/  Art/  Audio/  Materials/  Shaders/  Fonts/
│   │   ├── Starter Assets/                 imported package v2.0.2 (keep separate, edit minimally)
│   │   └── ThirdParty/                     other imported assets
│   ├── Packages/manifest.json              ← check here for exact package versions
│   └── ProjectSettings/
├── server/
│   ├── src/
│   │   ├── index.ts / app.config.ts        server bootstrap (follow the installed Colyseus version's template)
│   │   ├── rooms/RaceRoom.ts               the online match
│   │   ├── rooms/schema/RaceState.ts       synced state (schema)
│   │   ├── logic/                          pure, unit-testable functions: layout, validation, listener
│   │   └── levels/                         layout data shared with the client (JSON)
│   ├── test/                               unit tests for logic/ (node:test)
│   ├── Dockerfile · compose.yaml · .dockerignore · eslint.config.js
│   ├── package.json · package-lock.json · tsconfig.json · .env.example
├── docs/
│   ├── GDD.md · DESIGN.md · SYSTEM_ATLAS.md · CONVENTIONS.md · LAB5/LAB6_REQUIREMENTS.md · SETUP.md · IDEAS.md · CREDITS.md
│   ├── learning/gaps.md (wrap-up) · testing/QA-PROFILE.md
│   ├── pipeline.png     Lab 6 pipeline diagram
│   ├── sessions/        one short note per working session
│   ├── screenshots/     for README and paper
│   ├── playtest/        anonymized forms (CSV/PDF), observation sheets, analysis
│   └── paper-lab5.pdf · paper-lab6.pdf   IEEE papers (later)
└── planning/still-water/   MAP.md + decisions/
```

**Shared level data:** layouts are stored as JSON (`server/src/levels/*.json`) and copied into the client
(`client/Assets/_Project/Configs/Levels/`) by a small script, so client and server always agree
on platform positions. *(Draft — confirm in the [Netcode Model](../planning/still-water/decisions/10-netcode-model.md) decision.)*

---

## 3. Client systems (planned)

| System | Responsibility | Notes |
|---|---|---|
| `GameState` | Current mode (offline/online), current level, unlocked level, stars, last result, settings | Persistent singleton (DontDestroyOnLoad). The equivalent of the lab's Godot `GameState.gd`. |
| `SaveSystem` | Load/save progress + settings | See §5. Saves immediately after a level clear, **before** the result screen. |
| `SceneLoader` | Scene changes with a fade | One place for all scene names |
| `AudioManager` | Music/SFX across scenes, mute toggle | Persistent. Respects the saved settings. |
| `LevelController` | Runs one level: reveal → cross → win/lose, timer, splash count, stars | Reads a `LevelConfig` ScriptableObject. One reusable script for every level. |
| `PatternRevealer` | Glows the safe platforms in order | Uses the level's seed |
| `Platform` | Type (safe/fake/moving/sinking), sink behavior, glow | |
| `PlayerMovement` | Starter Assets controller + feel helpers + swim state | Values come from `MovementConfig` |
| `ListenerAI` | Dormant → Alerted → Hunting → Catch/Returning | Offline only. Online, the server drives it and the client just displays it. |
| `TouchControls` | Floating joystick + jump button + pause | Starter Assets v2 ships `TouchscreenInput` (UI Toolkit `UIDocument`: move + look sticks, jump, sprint, safe-area aware). Adapt it to a floating move stick in Phase 1. |
| `NetClient` | Connect, matchmake, join by code, send position, receive state, handle errors | Every failure → event → error screen |

## 4. Server systems (planned)

| Piece | Responsibility |
|---|---|
| `RaceRoom` | Max 2 clients. Phases: `waiting → countdown → reveal → racing → finished`. |
| `RaceState` (schema) | `phase`, `levelId`, `seed`, `players` map (`x,y,z,rotY,state,splashes,finishedAt`), `listener` (`x,z,targetId,state`), `winnerId` |
| `logic/validate.ts` | Rejects impossible moves (too fast, teleporting, standing on a sunk platform) |
| `logic/listener.ts` | Online Listener movement, running on a fixed server tick |
| `logic/layout.ts` | Seed → which platforms are safe (same algorithm as the client, or the server sends the result) |

### 4.1 Message protocol (Draft — finalized in [Netcode Model](../planning/still-water/decisions/10-netcode-model.md))

| Direction | Message | Payload | Rate |
|---|---|---|---|
| C → S | `pos` | `x,y,z,rotY,anim` | 15–20 Hz |
| C → S | `fell` | `x,z` | event |
| C → S | `climbed` | `platformId` | event |
| C → S | `reachedExit` | — | event |
| S → C | state sync | `RaceState` (schema patches) | Colyseus default patch rate |
| S → C | `caught` | `playerId, penaltySec` | event |
| S → C | `result` | `winnerId, times` | event |

Clients move themselves instantly. Remote players are drawn with interpolation (~100 ms buffer).

---

## 5. Save data (Draft — [Save Data](../planning/still-water/decisions/11-save-data.md))

JSON at `Application.persistentDataPath/save.json`. Survives force-close (lab R5).

```json
{
  "version": 1,
  "unlockedLevel": 1,
  "bestStars": { "1": 3, "2": 1 },
  "bestTimes": { "1": 42.3 },
  "settings": { "musicVol": 0.8, "sfxVol": 1.0, "vibration": true, "soundOn": true, "camSensitivity": 1.0, "textScale": 1.0 },
  "seenTutorial": false,
  "playerName": ""
}
```

Rules: write to a temp file, then replace the real file (so a crash mid-save can't corrupt it). Keep a hidden debug "Reset progress" button for testing.

---

## 6. Build and tooling

- **Unity version (pinned):** 6000.3.27f2 (Unity 6.3 LTS).
- **Unity Hub modules:** Android Build Support + OpenJDK + Android SDK & NDK Tools (installed and verified 2026-11-02).
- **Player settings:** package `com.itsd85.<team>.stillwater` (lowercase), orientation Auto Rotation with Landscape Left/Right only ([Orientation](../planning/still-water/decisions/03-orientation.md)), UI reference 1920×1080 + safe area, Internet permission (Unity adds it automatically when networking is used; verify), **Allow downloads over HTTP = Always allowed** (LAN servers are plain `http`/`ws`; Colyseus matchmakes over HTTP), IL2CPP + ARM64 for the release APK.
- **Output:** `.apk` for the lab (attached to the GitHub Release, never committed).
- **Git:** Unity `.gitignore` (excludes `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/`). Set Unity to **Visible Meta Files** + **Force Text** serialization. Consider Git LFS for large binaries (GitHub's free LFS quota is limited, so keep big source art out of the repo). Never commit `.keystore`/`.jks`, `.env`, builds.
- **Dev environment:** everything on Windows (see [SETUP.md](SETUP.md)). Repo `C:\Users\LEGION2\Yaji\projects\still-water`.
- **Server dev:** Node on Windows, `npm start` / `npm run dev` in `server/` (port 2567). The Unity Editor uses `ws://localhost:2567`; phones on the same Wi-Fi use `ws://<laptop LAN IP>:2567` (Windows Firewall rule for TCP 2567, Private profile). The server address should be a setting in the game, not hard-coded.
- **Server deploy:** automatic via the CI/CD pipeline to the Ubuntu VM (§8). *Optional:* the same Docker image on the Render free tier for internet play (spins down after 15 min idle and takes ~1 min to wake, so open the app once before a demo).
- **Testing two players in the Editor:** Unity 6 Multiplayer Play Mode, or one Editor + one phone.

## 7. Server endpoints (planned)

| Route | Purpose |
|---|---|
| `ws://…/` (Colyseus) | Game rooms (`RaceRoom`) |
| `GET /` | Small status page: "Still Water server · version `<APP_VERSION>` · rooms open". Lab 6's "change the homepage text" demo uses this. |
| `GET /health` | `{ "status": "ok", "version": "<APP_VERSION>" }`. `APP_VERSION` = the commit SHA in deploys, `dev` locally. |

Colyseus runs on Express, so these are normal Express routes (set them up where the installed Colyseus template configures Express).

## 8. CI/CD and deployment (Lab 6 — [Lab 6 Integration](../planning/still-water/decisions/13-lab6-integration.md))

```
laptop (Windows) ──git push / PR──► GitHub ──► job "test" (ubuntu-latest, cloud)
                                                  npm ci → lint → typecheck → test
                                               │ only if green AND push to main
                                               ▼
                         job "deploy" on the self-hosted runner (label: deploy)
                         inside the Ubuntu Server VM (VMware Workstation, same laptop)
                           docker compose -p stillwater up -d --build  (APP_VERSION = commit SHA)
                           health-check loop: curl localhost:8080/health must show the SHA
                           docker image prune -f
```

- **Workflow file:** `.github/workflows/ci-cd.yml` (repo root). Jobs use `defaults.run.working-directory: server`, and the setup-node npm cache uses `server/package-lock.json`.
  **No workflow-level `paths:` filter** (a skipped workflow never reports the required `test` check, which would block merging Unity-only PRs).
- **Docker:** `server/Dockerfile` (node:24-alpine, `npm ci`, build TS, `ARG/ENV APP_VERSION`, non-root `USER node`, `HEALTHCHECK` on `/health`), `server/compose.yaml` (image `stillwater-server:${APP_VERSION}`, ports **`8080:2567`**, `restart: unless-stopped`). Images are tagged per commit for rollback.
- **VM (VMware Workstation, NAT on VMnet8) port forwards** (Virtual Network Editor → VMnet8 → NAT Settings; the guest port points at the VM's IP):

  | Name | Host port | Guest port | Use |
  |---|---|---|---|
  | ssh | 2222 | 22 | `ssh -p 2222 deploy@localhost` |
  | game | 8080 | 8080 | Deployed game server: status page, `/health`, and **game WebSocket** |

  Phones on the same Wi-Fi reach the deployed server at `ws://<laptop LAN IP>:8080` (Windows Firewall: allow inbound TCP 8080, Private).
  The local dev server stays on `:2567`, so both can run at the same time.
- **Server addresses the game knows:** `Local dev` (`ws://localhost:2567` / LAN IP:2567), `LAN VM` (LAN IP:8080), optional `Cloud` (Render `wss://`). Chosen in the game's settings or lobby.
- **Branch ruleset `protect-main`:** PR required (0 approvals when solo), required status check `test`, block force pushes.
- **Repo settings:** public; Actions → "Require approval for all external contributors" (protects the self-hosted runner).
- **Rollback:** Method 1 = `git revert` through a PR; Method 2 = in the runner's work folder (`~/_work/still-water/still-water/server` on the VM), `APP_VERSION=<old sha> docker compose -p stillwater up -d --no-build` (measured 0.48 s). Method 2 is temporary: the next merge to `main` redeploys the latest SHA.
- **Tests:** `server/test/` on pure functions in `server/src/logic/` (see [Server Test and Lint Tooling](../planning/still-water/decisions/14-server-test-tooling.md)).

## 9. Glossary
- **Safe / Fake platform:** part of the memorized path / sinks when stood on.
- **Reveal:** the pattern-glow phase at level start.
- **Splash:** falling into the water. Wakes the Listener.
- **The Listener:** the monster (working name).
- **Seed:** a number that decides which platforms are safe. The same seed gives the same path.
