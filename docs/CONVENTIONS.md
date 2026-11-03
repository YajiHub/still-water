# Conventions — rules, dev environment and workflow

> Detail behind [AGENTS.md](../AGENTS.md). AGENTS.md stays short; the full rules live here.

## 1. Rules for every AI session

1. **Read before asking.** Check this file, the MAP, and the latest session note before asking the user
   anything they already answered.
2. **Never guess third-party APIs.** Unity 6, Cinemachine 3, the Input System and Colyseus have all
   changed APIs between versions. Check the installed version (Unity: `Packages/manifest.json`;
   server: `server/package.json`) and the official docs for that version before writing code.
3. **Respect the plan.** Do not implement items marked *Open* in the MAP as if they were decided.
   If you must assume, say so in your reply and add the question to the MAP's Active Frontier.
4. **Draft, not law.** Numbers in DESIGN.md (speeds, timings, sizes) are starting values meant to be
   tuned by playtesting. Expose them as serialized fields / ScriptableObjects, never hard-code them.
5. **Offline must never touch the network.** Offline mode must work in airplane mode with no
   network calls, no waiting and no error pop-ups (lab requirement R6).
6. **Online must fail safely.** Every network failure ends on a clear message and a way back to
   Mode Select — never a crash or frozen screen (R8).
7. **The server decides outcomes online.** Clients send inputs/positions; the server decides
   splashes, catches, finishes and the winner.
8. **Assets:** only original or properly licensed assets (CC0 / CC-BY / OFL). Add every non-original
   asset to [CREDITS.md](CREDITS.md) in the same change that adds the asset. No characters,
   music or logos from commercial games or films (that includes the Roblox game that inspired this).
9. **Secrets:** never commit keystores (`.keystore`, `.jks`), passwords, `.env` files or server keys.
10. **Keep the pipeline green.** Server game rules live in pure functions in `server/src/logic/` with tests in
    `server/test/`. Run `npm run lint`, `npm run typecheck` and `npm test` in `server/` before saying a server change
    is done. Never commit the runner token, `.env` files or SSH private keys.
11. **Keep docs current, overwrite in place.** When a decision changes, edit the existing text;
    do not append "update:" paragraphs. At the end of a session, add a short note in `docs/sessions/`.

## 2. Dev environment: everything runs on Windows

- **Everything builds and runs on Windows:** Unity Hub + Unity Editor 6000.3.27f2, Node.js + the Colyseus
  server, Git, the Android phone over USB. Repo: `C:\Users\LEGION2\Yaji\projects\still-water`.
- **The AI agent may run from either side:**
  - Windows (e.g. Claude Code in PowerShell/Windows Terminal) → paths like `C:\Users\LEGION2\Yaji\projects\still-water\...`
  - WSL (e.g. pi) → the same repo at `/mnt/c/Users/LEGION2/Yaji/projects/still-water`. From WSL, run commands that must
    execute on Windows (npm for the server, Unity) through the human, or with `cmd.exe /c` / `powershell.exe`,
    rather than installing a second Linux copy of Node or the Android SDK.
- **Git:** works from either side because the repo has `.gitattributes` and both sides use
  `core.autocrlf=false` and `core.fileMode=false` (see [SETUP.md](SETUP.md)). Don't commit
  unless the human asks.
- **Never create, edit or delete `.meta` files.** Unity generates them when it sees new files. Moving or
  renaming assets must be done in the Unity Editor (or the human must move the `.meta` with the file),
  or references break.
- **You cannot see the Unity Editor.** For anything done in the Editor (scene setup, Inspector wiring,
  prefabs, colliders, layers, project settings), give the human short numbered steps instead of editing
  `.unity`/`.prefab`/`.asset` YAML by hand. Editor scripts (menu items that build objects for the human)
  are a good alternative when the steps are long.
- **Unity compile errors:** ask the human to paste them from the Console, or read the Editor log at
  `C:\Users\LEGION2\AppData\Local\Unity\Editor\Editor.log` (WSL: `/mnt/c/Users/LEGION2/AppData/Local/Unity/Editor/Editor.log`).
- **Server (dev):** runs with Node on Windows, default port **2567**. Phones on the same Wi-Fi reach it at the
  laptop's LAN IP. Windows Firewall must allow inbound TCP 2567 (Private network).
- **Server (deployed):** the Ubuntu Server VM (VMware Workstation, NAT on VMnet8) runs the GitHub self-hosted runner + Docker. Port
  forwards: host 2222 → ssh, host 8080 → the deployed server. You can't reach the VM; the human runs VM commands
  (`ssh -p 2222 deploy@localhost`). Details: [SYSTEM_ATLAS.md §8](SYSTEM_ATLAS.md#8-cicd-and-deployment-lab-6--lab-6-integration).
- **Branch protection:** once the `protect-main` ruleset is on, changes reach `main` only through PRs with a
  green `test` check. Work on branches (`feature/...`, `fix/...`).
- **Unity Hub quirk:** this Hub is installed as a sandboxed Windows app, so downloaded templates live under
  `%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_...\LocalCache\Roaming\UnityHub\Templates`, junction-linked
  to `%APPDATA%\UnityHub\Templates`.

## 3. Working mode: AI builds, human reviews (with ownership tags)

The human uses AI heavily (including vibe coding) but stays **in the loop as reviewer, tester and decision-maker**,
and is new to Unity and C#. The workflow uses the human's own skills (in `.claude/skills/` for Claude Code,
globally linked in `~/.pi/agent/skills/` for pi):

| When | Command (pi: `/skill:<name>` · Claude Code: `/<name>`) |
|---|---|
| Start of a session / "what's next?" | `wayfinder what's next?` (status check) |
| Resolve an open decision | `wayfinder continue planning/still-water/MAP.md` |
| Build one phase | `execute-phase Phase <N>` |
| End of a session | `wrap-up` (verify → ownership check → session note → `docs/learning/gaps.md` → status) |
| Trace how a feature works end to end | `codebase-xray` |
| You write the code and the AI coaches | `pair-navigator` |

**Ownership tags** on every plan task: 🧑 human writes · 🤝 together (agent writes skeleton + test, human writes the body) ·
🤖 agent writes. Default is 🤖, with 1–2 small core-logic tasks per phase as 🧑/🤝. The human can retag anything.
Unity Editor work (scenes, Inspector wiring, project settings, builds) is 🧑 by nature: the agent gives numbered steps.

For 🤖 work:
- Small, single-feature steps from the MAP. Before a change: one or two lines on what and why.
- After a change: files touched, Editor steps for the human (numbered), how to test it (what they should see),
  and **one short "worth knowing" note** on the key Unity/C#/networking concept.
- Readable code for a beginner: clear names, small classes, comments on the *why* for state machines,
  jump assists and netcode. No clever one-liners.
- Decisions stay with the human: gameplay, design and architecture choices go through the MAP (Wayfinder).
  Locked decisions record who made them: `(by: me)` or `(by: delegated)`.
