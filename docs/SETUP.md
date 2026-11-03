# Setup — Windows (first run)

> A step-by-step checklist for getting from "tools installed" to Phase 0 done.
> Everything runs on Windows. An AI agent can follow along and guide, but the Unity Editor steps are done by hand.
> Tick the boxes as you go, and edit freely.

## 0. Tools (check once)

- [x] Unity Hub + **Unity 6000.3.27f2 (6.3 LTS)** with Android Build Support, OpenJDK, Android SDK & NDK
- [x] Hub template fix (junction). See the "Unity Hub quirk" in [CONVENTIONS.md §2](CONVENTIONS.md#2-dev-environment-everything-runs-on-windows)
- [x] Node.js 24 LTS: `node -v` in PowerShell (v24.13.0, npm 11.19.0). WSL has its own Node 22: don't use it for this repo.
- [x] Git for Windows: `git --version` (2.52.0)
- [x] Code editor: VS Code + C# Dev Kit + Unity extension
- [ ] AI agent: Claude Code (Windows) and/or pi (WSL, via `/mnt/c/Users/LEGION2/Yaji/projects/still-water`)
- [ ] *(Optional, later)* Docker Desktop, only to test the deploy image locally. The VM (§7.4) is where the image really runs.

## 1. Repo folder

- [x] Project docs are in `C:\Users\LEGION2\Yaji\projects\still-water` (so `AGENTS.md` is at the root).
- [x] In PowerShell:
  ```powershell
  cd C:\Users\LEGION2\Yaji\projects\still-water
  git init -b main
  git config core.autocrlf false
  git config core.fileMode false
  ```
  These settings live in the repo, so Git from WSL behaves the same way. `.gitignore` and `.gitattributes` are already in the pack.

## 2. Unity project

- [x] Unity Hub → **New project** → Editor **6000.3.27f2** → template **Universal 3D**
- [x] Project name **`client`**, Location **`C:\Users\LEGION2\Yaji\projects\still-water`** → Unity creates `C:\Users\LEGION2\Yaji\projects\still-water\client`
- [x] Untick *Connect to Unity Cloud*, *Unity Version Control*, *Use AI Assistant* → **Create project**
- [x] Add Unity's ignore file inside `client/` (its rules are relative to the Unity project):
  ```powershell
  Invoke-WebRequest https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -OutFile C:\Users\LEGION2\Yaji\projects\still-water\client\.gitignore
  ```
- [x] **Edit → Project Settings → Editor:** Version Control = *Visible Meta Files*, Asset Serialization = *Force Text* (usually already set)
- [ ] **Edit → Preferences → External Tools:** pick Rider or VS Code

## 3. Android + first phone build

- [x] **File → Build Profiles → Android → Switch Platform**
- [ ] **Project Settings → Player (Android tab):** Company Name, Product Name `Still Water`, Package Name `com.itsd85.<yourname>.stillwater`, Resolution and Presentation → **Default Orientation = Auto Rotation**, allowed: only **Landscape Right** + **Landscape Left** ([Orientation](../planning/still-water/decisions/03-orientation.md))
- [x] Phone: **Settings → About phone → tap Build number 7×** → Developer options → **USB debugging** on → plug in → allow
- [x] Build Profiles → Android → **Run Device** = your phone → **Build And Run** → save to `C:\Users\LEGION2\Yaji\projects\still-water\Builds\` (git-ignored). The first build takes 5–15 min.
- [x] ✅ The default URP sample scene opens on the phone

## 4. Starter Assets (the player controller base)

- [x] Browser: Asset Store → **Starter Assets: First Person + Third Person | Character Controllers** (v2.0.2, Unity 6, Cinemachine 3) → *Add to My Assets*
- [x] Unity: **Window → Package Manager → My Assets** → Download → Import. If it asks to enable the new Input System and restart: **Yes**
- [x] Open `Assets/Starter Assets/Sample/ThirdPersonController/Playground` → **Play** → WASD + Space work
- [ ] Troubleshooting:
  - Pink materials → **Window → Rendering → Render Pipeline Converter → Material Upgrade → Convert**
  - Red Cinemachine errors → paste them to the AI agent (the package was made for an older Unity)
- [ ] Add the Playground scene to the Build Profile scene list → **Build And Run**
- [ ] ✅ The character runs on the phone with the on-screen joystick + jump button

## 5. First commit + GitHub

- [ ] `git add . && git commit -m "Unity 6.3 project, Starter Assets, planning docs"`
- [x] **Public** GitHub repo [`YajiHub/still-water`](https://github.com/YajiHub/still-water) (shared with Lab 6; see [Lab 6 Integration](../planning/still-water/decisions/13-lab6-integration.md)), remote `origin` = `yaji.github.com:YajiHub/still-water.git` (SSH host alias in `~/.ssh/config`)
- [x] `git push -u origin main`
- [ ] ✅ GitHub shows `client/Assets`, and **no** `client/Library`

## 6. Hello server

- [x] PowerShell (scaffolded with create-colyseus-app 0.18.1 → colyseus 0.18.10; `npm install` runs automatically):
  ```powershell
  cd C:\Users\LEGION2\Yaji\projects\still-water
  npm create colyseus-app@0.18.1 ./server -- --ts --preset minimal --yes
  cd server; npm start
  ```
- [x] Browser `http://localhost:2567` loads → the server is running
- [x] Firewall (PowerShell **as Administrator**, once). Also make sure your Wi-Fi is set to a *Private* network:
  ```powershell
  New-NetFirewallRule -DisplayName "Colyseus 2567" -Direction Inbound -Protocol TCP -LocalPort 2567 -Action Allow -Profile Private
  ```
- [x] Unity: **Package Manager → + → Add package from git URL** → `https://github.com/colyseus/colyseus-unity-sdk.git#upm` (`io.colyseus.sdk` 0.18.10, commit `0bb05cb`, matching the server). **Now embedded** in `client/Packages/io.colyseus.sdk`: upstream ships `Runtime/Example/Schema` as a symlink, which Windows checks out as a plain file with no `.meta`, and Android builds fail (`has no meta file, but it's in an immutable folder`). The embedded copy is identical except that one file is removed. To update the SDK later: re-copy from `Library/PackageCache` and delete that file again.
- [x] **Project Settings → Player → Other Settings → Allow downloads over HTTP = Always allowed.** Unity 6 blocks plain HTTP by default, and Colyseus matchmakes over HTTP before opening the WebSocket (error: `Insecure connection not allowed`).
- [x] `NetTest` script (`Assets/_Project/Scripts/Net/NetTest.cs`) in scene `Assets/_Project/Scenes/NetTest.unity` joins `my_room` → Play → the Console prints *Joined room*
- [x] On the phone: use the laptop's Wi-Fi IP (`ipconfig` → the **Wi-Fi** adapter's IPv4, here `ws://192.168.1.9:2567`; ignore the VMware/WSL/ZeroTier adapters) → ✅ joins from the phone
- [ ] Commit

## Phase 0 done when

- [ ] Character runs on the phone · [ ] server reachable from the phone · [ ] repo on GitHub
- [x] [Orientation](../planning/still-water/decisions/03-orientation.md) locked · [ ] instructor asked about Option A ([Hosting and LAN Plan](../planning/still-water/decisions/12-hosting-and-lan.md))
- [ ] GDD + paper-prototype photo submitted (Checkpoint 1)

Then update the Phase table in the [MAP](../planning/still-water/MAP.md) and do **Phase 0b (§7)**, then **Phase 1: Feel prototype**.

## 7. CI/CD pipeline (Lab 6)

> Full requirements: [LAB6_REQUIREMENTS.md](LAB6_REQUIREMENTS.md). Design: [SYSTEM_ATLAS.md §8](SYSTEM_ATLAS.md#8-cicd-and-deployment-lab-6--lab-6-integration).
> Take the checkpoint screenshots (CP1–CP6) as you go and save them to `docs/screenshots/`.

**7.1 Local (Lab 6 Part 1)**
- [x] Git identity + SSH key on GitHub; remote is SSH via the host alias (`yaji.github.com:YajiHub/still-water.git`, `~/.ssh/config`).
- [ ] 📸 CP1 in PowerShell: `node -v`, `npm -v`, `git --version`, `ssh -T yaji.github.com` (the alias, so the right key is used)

**7.2 Server ready for CI (Part 2)**, which the AI agent can do (see [Server Test and Lint Tooling](../planning/still-water/decisions/14-server-test-tooling.md))
- [x] `GET /` status page + `GET /health` → `{status, version}` from `APP_VERSION`
- [x] First pure logic function `isMovePlausible` in `server/src/logic/movement.ts` + tests in `server/test/movement.test.ts` (`node:test` + `tsx`, see the decision)
- [x] ESLint (typescript-eslint), `typecheck`, `test` scripts in `server/package.json`; `package-lock.json` committed
- [x] `server/Dockerfile`, `server/compose.yaml` (`8080:2567`, image tagged with `APP_VERSION`), `.dockerignore`
- [x] Locally: `npm run lint`, `npm run typecheck`, `npm test` all pass; `http://localhost:2567/` shows version `dev`. 📸 CP2

**7.3 GitHub (Parts 3–4)**
- [x] The repo is **public**; Settings → Actions → General → fork PR approval = *Require approval for all external contributors*. 📸 CP3
- [x] `.github/workflows/ci-cd.yml` with the `test` job (working directory `server`) → push → green in the Actions tab. 📸 CP4

**7.4 Ubuntu Server VM (Part 5)**
- [x] VMware Workstation (already installed; replaces the handout's VirtualBox) + Ubuntu Server **26.04.1 LTS** (handout: 24.04 or newer). VM IP on VMnet8: `192.168.95.132`. VM `stillwater-vm`: 2 vCPU, 4 GB RAM, 25 GB, network **NAT** (VMnet8). Port forwards in Edit → Virtual Network Editor → VMnet8 → NAT Settings: ssh `2222 → VM-IP:22`, game `8080 → VM-IP:8080`
- [x] Install with user `deploy` + OpenSSH; then `ssh -p 2222 deploy@localhost`
- [x] `sudo apt install -y docker.io docker-compose-v2 git curl`, enable docker, `usermod -aG docker deploy`, log in again, `docker run --rm hello-world`
- [x] Manual dry run: clone, `APP_VERSION=manual docker compose -p stillwater up -d --build` (in `server/`), then open `http://localhost:8080/health` in the Windows browser. 📸 CP5. Then `down` and delete the clone.
- [ ] Windows Firewall: allow inbound TCP **8080** (Private), so phones can reach the deployed server

**7.5 Self-hosted runner + deploy job (Part 6)**
- [x] GitHub → Settings → Actions → Runners → New self-hosted runner (Linux x64) → run the shown commands in the VM, with `--labels deploy`, then `sudo ./svc.sh install deploy && sudo ./svc.sh start`
- [x] Add the `deploy` job (`needs: test`, main push only, `concurrency`, `APP_VERSION: ${{ github.sha }}`, compose up `--build`, health-check loop, prune) → push → test → deploy green → the browser shows the SHA. 📸 CP6 (runner Idle, green run, SHA matches)
- [ ] Phone check: the game connects to `ws://<laptop LAN IP>:8080`

**7.6 End-to-end (Part 7)**
- [ ] Ruleset `protect-main` (PR required, status check `test`, block force pushes)
- [ ] **Test A:** branch → change the status-page text → PR → merge → deployed (record the time)
- [ ] **Test B:** branch → break a logic function (e.g. the winner decision) → PR → `test` red, merge blocked → close the PR
- [ ] **Test C:** merge an obvious bad text change → roll back via revert PR (Method 1) and via `--no-build` with an old SHA (Method 2)
- [ ] Fill in the results table in [LAB6_REQUIREMENTS.md](LAB6_REQUIREMENTS.md); collect ≥ 5 deploy timings for the paper
