# QA Profile — Still Water

qa-tester reads this first. Keep it current when a run command, port or rule changes.
Game rules live in [DESIGN.md](../DESIGN.md); lab acceptance checks in [LAB5](../LAB5_REQUIREMENTS.md) / [LAB6](../LAB6_REQUIREMENTS.md).

## What an agent can and cannot test
| Area | Agent can | Needs the human |
|---|---|---|
| Server (`server/`) | Run `npm test`, start it, hit HTTP routes, join rooms with `@colyseus/testing` or the JS SDK | — |
| Unity client | Read code and `Editor.log` | Press Play, read the Console, build to the phone, touch input, visuals, audio, fps |
| CI/CD | Read workflow files and Actions logs (via `gh` when logged in) | Anything inside the VM (`ssh -p 2222 deploy@localhost`) |

## How to run
All Node commands run on **Windows Node 24** (from WSL: `cmd.exe /c` / `powershell.exe`), never a Linux Node.
- **Server (dev):** `cd server && npm start` → `http://localhost:2567`. Routes: `/hi` (text), `/api/hello` (`{"message":"Hello World"}`), `/` (Colyseus playground, dev only), `/monitor` (dev only). Room: `my_room` (until `RaceRoom` exists).
- **Server tests:** `cd server && npm test`. Check the **exit code**, not only the summary; see [Server Test and Lint Tooling](../../planning/still-water/decisions/14-server-test-tooling.md) for the Windows mocha crash.
- **Server (deployed, after Phase 0b):** `http://localhost:8080/health` → `{"status":"ok","version":"<commit sha>"}`.
- **Unity Editor:** open `client/` in Unity 6000.3.27f2. Smoke scene `Assets/_Project/Scenes/NetTest.unity` → Play → Console `[NetTest] Joined room …`. Player test scene `Assets/Starter Assets/Sample/ThirdPersonController/Playground`.
- **Phone:** Android over USB, Build And Run → `Builds/Still Water.apk`. LAN server address `ws://<laptop Wi-Fi IP>:2567` (2026-11-03: `192.168.1.9`; ignore the VMware/WSL/ZeroTier adapters).
- **Logs:** server: the terminal running `npm start`. Unity: `C:\Users\LEGION2\AppData\Local\Unity\Editor\Editor.log`. Phone: Unity Android Logcat.

## Test data
No accounts or database. Progress lives in `save.json` under `Application.persistentDataPath` (after Phase 2). To start fresh: the debug "Reset progress" button, or clear the app's storage on the phone. Never edit a tester's save by hand.

## Protocol conventions
- Matchmaking is HTTP (`POST /matchmake/...`), then the room is a WebSocket. Plain `http`/`ws` needs **Allow downloads over HTTP = Always allowed** in Player Settings.
- Close codes seen so far: `4000` = client called `Leave()` cleanly · `1006` = connection dropped (app closed or backgrounded, Wi-Fi lost).
- Messages and state: [SYSTEM_ATLAS §4.1](../SYSTEM_ATLAS.md#41-message-protocol-draft--finalized-in-netcode-model) (Draft until [Netcode Model](../../planning/still-water/decisions/10-netcode-model.md) is locked).

## Rules worth probing
- **Offline never touches the network (R6):** airplane mode → offline play has no waits, no errors, no network calls.
- **Online fails safely (R8):** server down, wrong IP, Wi-Fi off mid-match → a clear message + back to Mode Select, never a freeze or crash.
- **The server decides (online):** splashes, catches, finish and winner come from the server; a client claiming `reachedExit` far from the exit must be rejected.
- **Unlocks + save (R4–R5):** clearing level n unlocks n+1; force-close and reopen keeps it; clearing L5 doesn't unlock "L6"; best stars/time keep the max.
- **Orientation (R9):** landscape only, flips Left↔Right, never portrait.
- **Disconnect:** a player leaving mid-race → the other wins by forfeit and sees a message.

## Out of bounds
- Never run `docker compose down`, prune, or redeploy on the VM, or touch the self-hosted runner, without the human.
- Never push, merge, or change GitHub settings/rulesets.
- Never edit `.meta`, `.unity`, `.prefab` or `.asset` files; report Editor steps instead.
- Never commit test artefacts, `.env` files, keystores or tokens.
- Never point a test at the Render (cloud) server except to read `/health`.
