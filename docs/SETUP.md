# Setup — Windows (first run)

> A step-by-step checklist for getting from "tools installed" to Phase 0 done.
> Everything runs on Windows. An AI agent can follow along and guide, but the Unity Editor steps are done by hand.
> Tick the boxes as you go, and edit freely.

## 0. Tools (check once)

- [x] Unity Hub + **Unity 6000.3.27f2 (6.3 LTS)** with Android Build Support, OpenJDK, Android SDK & NDK
- [x] Hub template fix (junction). See the "Unity Hub quirk" in [AGENTS.md](../AGENTS.md)
- [ ] Node.js LTS: `node -v` in PowerShell
- [ ] Git for Windows: `git --version`
- [ ] Code editor: JetBrains Rider (free for non-commercial use) or VS Code + C# Dev Kit + Unity extension
- [ ] AI agent: Claude Code (Windows) and/or pi (WSL, via `/mnt/c/Users/LEGION2/Yaji/projects/still-water`)
- [ ] *(Optional, later)* Docker Desktop, only to test the deploy image locally. Render builds the Dockerfile itself.

## 1. Repo folder

- [x] Project docs are in `C:\Users\LEGION2\Yaji\projects\still-water` (so `AGENTS.md` is at the root).
- [ ] In PowerShell:
  ```powershell
  cd C:\Users\LEGION2\Yaji\projects\still-water
  git init -b main
  git config core.autocrlf false
  git config core.fileMode false
  ```
  These settings live in the repo, so Git from WSL behaves the same way. `.gitignore` and `.gitattributes` are already in the pack.

## 2. Unity project

- [ ] Unity Hub → **New project** → Editor **6000.3.27f2** → template **Universal 3D**
- [ ] Project name **`client`**, Location **`C:\Users\LEGION2\Yaji\projects\still-water`** → Unity creates `C:\Users\LEGION2\Yaji\projects\still-water\client`
- [ ] Untick *Connect to Unity Cloud*, *Unity Version Control*, *Use AI Assistant* → **Create project**
- [ ] Add Unity's ignore file inside `client/` (its rules are relative to the Unity project):
  ```powershell
  Invoke-WebRequest https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -OutFile C:\Users\LEGION2\Yaji\projects\still-water\client\.gitignore
  ```
- [ ] **Edit → Project Settings → Editor:** Version Control = *Visible Meta Files*, Asset Serialization = *Force Text* (usually already set)
- [ ] **Edit → Preferences → External Tools:** pick Rider or VS Code

## 3. Android + first phone build

- [ ] **File → Build Profiles → Android → Switch Platform**
- [ ] **Project Settings → Player (Android tab):** Company Name, Product Name `Still Water`, Package Name `com.itsd85.<yourname>.stillwater`, **Default Orientation** per [Orientation](../planning/still-water/decisions/03-orientation.md) (proposed: Landscape Left)
- [ ] Phone: **Settings → About phone → tap Build number 7×** → Developer options → **USB debugging** on → plug in → allow
- [ ] Build Profiles → Android → **Run Device** = your phone → **Build And Run** → save to `C:\Users\LEGION2\Yaji\projects\still-water\Builds\` (git-ignored). The first build takes 5–15 min.
- [ ] ✅ The default URP sample scene opens on the phone

## 4. Starter Assets (the player controller base)

- [ ] Browser: Asset Store → **Starter Assets – Third Person** → *Add to My Assets*
- [ ] Unity: **Window → Package Manager → My Assets** → Download → Import. If it asks to enable the new Input System and restart: **Yes**
- [ ] Open `Assets/StarterAssets/ThirdPersonController/Scenes/Playground` → **Play** → WASD + Space work
- [ ] Troubleshooting:
  - Pink materials → **Window → Rendering → Render Pipeline Converter → Material Upgrade → Convert**
  - Red Cinemachine errors → paste them to the AI agent (the package was made for an older Unity)
- [ ] Add the Playground scene to the Build Profile scene list → **Build And Run**
- [ ] ✅ The character runs on the phone with the on-screen joystick + jump button

## 5. First commit + GitHub

- [ ] `git add . && git commit -m "Unity 6.3 project, Starter Assets, planning docs"`
- [ ] Create a **public** GitHub repo, proposed name `itsd85-lab5-6-<section>-<surname>` (shared with Lab 6; see [Lab 6 Integration](../planning/still-water/decisions/13-lab6-integration.md)), then `git remote add origin <url>` and `git push -u origin main`
- [ ] ✅ GitHub shows `client/Assets`, and **no** `client/Library`

## 6. Hello server

- [ ] PowerShell:
  ```powershell
  cd C:\Users\LEGION2\Yaji\projects\still-water
  npm create colyseus-app@latest ./server
  cd server; npm install; npm start
  ```
- [ ] Browser `http://localhost:2567` loads → the server is running
- [ ] Firewall (PowerShell **as Administrator**, once). Also make sure your Wi-Fi is set to a *Private* network:
  ```powershell
  New-NetFirewallRule -DisplayName "Colyseus 2567" -Direction Inbound -Protocol TCP -LocalPort 2567 -Action Allow -Profile Private
  ```
- [ ] Unity: **Package Manager → + → Add package from git URL** → `https://github.com/colyseus/colyseus-unity-sdk.git#upm`
- [ ] Ask the AI agent for a minimal `NetTest` script that joins the server's default room (check the room name in `server/src/app.config.ts` and the installed SDK's API) → Play → the Console prints *Joined room*
- [ ] On the phone: use the laptop's LAN IP (`ipconfig` → IPv4, e.g. `ws://192.168.1.23:2567`) → ✅ joins from the phone
- [ ] Commit

## Phase 0 done when

- [ ] Character runs on the phone · [ ] server reachable from the phone · [ ] repo on GitHub
- [ ] [Orientation](../planning/still-water/decisions/03-orientation.md) locked · [ ] instructor asked about Option A ([Hosting and LAN Plan](../planning/still-water/decisions/12-hosting-and-lan.md))
- [ ] GDD + paper-prototype photo submitted (Checkpoint 1)

Then update the Phase table in the [MAP](../planning/still-water/MAP.md) and do **Phase 0b (§7)**, then **Phase 1: Feel prototype**.

## 7. CI/CD pipeline (Lab 6)

> Full requirements: [LAB6_REQUIREMENTS.md](LAB6_REQUIREMENTS.md). Design: [SYSTEM_ATLAS.md §8](SYSTEM_ATLAS.md#8-cicd-and-deployment-lab-6--lab-6-integration).
> Take the checkpoint screenshots (CP1–CP6) as you go and save them to `docs/screenshots/`.

**7.1 Local (Lab 6 Part 1)**
- [ ] Git identity + SSH key: `ssh-keygen -t ed25519`, add the public key on GitHub, then `ssh -T git@github.com` → switch the remote to `git@github.com:<you>/<repo>.git`. 📸 CP1 (`node -v`, `npm -v`, `git --version`, ssh -T)

**7.2 Server ready for CI (Part 2)**, which the AI agent can do (see [Server Test and Lint Tooling](../planning/still-water/decisions/14-server-test-tooling.md))
- [ ] `GET /` status page + `GET /health` → `{status, version}` from `APP_VERSION`
- [ ] First pure logic function in `server/src/logic/` + tests in `server/test/`
- [ ] ESLint (typescript-eslint), `typecheck`, `test` scripts in `server/package.json`; `package-lock.json` committed
- [ ] `server/Dockerfile`, `server/compose.yaml` (`8080:2567`, image tagged with `APP_VERSION`), `.dockerignore`
- [ ] Locally: `npm run lint`, `npm run typecheck`, `npm test` all pass; `http://localhost:2567/` shows version `dev`. 📸 CP2

**7.3 GitHub (Parts 3–4)**
- [ ] The repo is **public**; Settings → Actions → General → fork PR approval = *Require approval for all external contributors*. 📸 CP3
- [ ] `.github/workflows/ci-cd.yml` with the `test` job (working directory `server`) → push → green in the Actions tab. 📸 CP4

**7.4 Ubuntu Server VM (Part 5)**
- [ ] VirtualBox + Ubuntu Server 24.04 LTS ISO. VM: 2 vCPU, 2–4 GB RAM, 25 GB. Port forwards: **ssh 2222→22**, **game 8080→8080**
- [ ] Install with user `deploy` + OpenSSH; then `ssh -p 2222 deploy@localhost`
- [ ] `sudo apt install -y docker.io docker-compose-v2 git curl`, enable docker, `usermod -aG docker deploy`, log in again, `docker run --rm hello-world`
- [ ] Manual dry run: clone, `APP_VERSION=manual docker compose -p stillwater up -d --build` (in `server/`), then open `http://localhost:8080/health` in the Windows browser. 📸 CP5. Then `down` and delete the clone.
- [ ] Windows Firewall: allow inbound TCP **8080** (Private), so phones can reach the deployed server

**7.5 Self-hosted runner + deploy job (Part 6)**
- [ ] GitHub → Settings → Actions → Runners → New self-hosted runner (Linux x64) → run the shown commands in the VM, with `--labels deploy`, then `sudo ./svc.sh install deploy && sudo ./svc.sh start`
- [ ] Add the `deploy` job (`needs: test`, main push only, `concurrency`, `APP_VERSION: ${{ github.sha }}`, compose up `--build`, health-check loop, prune) → push → test → deploy green → the browser shows the SHA. 📸 CP6 (runner Idle, green run, SHA matches)
- [ ] Phone check: the game connects to `ws://<laptop LAN IP>:8080`

**7.6 End-to-end (Part 7)**
- [ ] Ruleset `protect-main` (PR required, status check `test`, block force pushes)
- [ ] **Test A:** branch → change the status-page text → PR → merge → deployed (record the time)
- [ ] **Test B:** branch → break a logic function (e.g. the winner decision) → PR → `test` red, merge blocked → close the PR
- [ ] **Test C:** merge an obvious bad text change → roll back via revert PR (Method 1) and via `--no-build` with an old SHA (Method 2)
- [ ] Fill in the results table in [LAB6_REQUIREMENTS.md](LAB6_REQUIREMENTS.md); collect ≥ 5 deploy timings for the paper
