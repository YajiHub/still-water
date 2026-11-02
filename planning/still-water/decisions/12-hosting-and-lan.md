# Hosting and LAN Plan

- **Status:** Open
- **Type:** research (+ one question for the instructor)
- **Blocks:** Phase 0 (server skeleton), Phase 4

## Question
Where does the Colyseus server run for development, for the "same Wi-Fi" requirement (Option A), and for internet play (Option B)?

> Context from [Lab 6 Integration](./13-lab6-integration.md): the CI/CD pipeline deploys the server to an Ubuntu VM on the laptop. That VM is the natural same-Wi-Fi server (phones → laptop IP:8080). Render is now only needed for internet play.

## Facts (verified 2026-10-27; re-check before deploying)
- Render free web service: spins down after **15 min** without traffic (HTTP or WebSocket messages); waking takes **~1 min**; 750 free instance hours/month. WebSocket messages on an open connection count as activity.
- Colyseus: MIT-licensed Node/TS server with an official Unity SDK (installed via the Package Manager git URL). The current major line was 0.18 at the time of writing. Match the SDK and server versions.
- The lab's Option A, as written: "one phone hosts, the other joins by IP, no server needed". Ours uses a laptop server, which **differs**.

## Options for Option A
| Option | For | Against |
|---|---|---|
| **Laptop runs Colyseus on the same Wi-Fi/hotspot; phones join by IP/room code**: either the dev server (:2567) or the pipeline-deployed VM (:8080) | Zero extra code; same server as online; the VM version is also the Lab 6 deployment | Not "phone hosts" literally; needs instructor OK |
| A phone hosts (Unity Netcode for GameObjects + direct IP) | Matches the handout literally | A second networking stack to build and maintain, roughly doubling online work |
| Run the Colyseus server on an Android phone (e.g. Termux/Node) | Technically "phone hosts" | Fragile; not worth it |

## Plan
1. **Ask the instructor this week:** "Our online mode uses a dedicated Colyseus server. For the same-Wi-Fi requirement, can we run it on a laptop in the room, plus a cloud deployment for internet play?"
2. Dev: `npm run dev` on Windows (:2567); Unity Editor + phone connect via the laptop's LAN IP.
3. Same-Wi-Fi demo: the pipeline-deployed server in the VM (`ws://<laptop LAN IP>:8080`).
4. Internet (Option B, optional): the same Docker image on the Render free tier (wss). Before demos, open the app once to wake it.
5. In-app: Settings or Lobby has a server picker (Local dev / LAN VM / Cloud, or enter an address) so the demo can switch.

## Decision
_(fill in after the instructor answers)_
